using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace PeliAsiakas
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket palvelin = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 9999);
            EndPoint pep = (IPEndPoint)ep;

            Console.WriteLine("Tervetuloa pelaamaan numeronarvauspeliä, jossa pelaajan on tarkoitus" + 
                               "arvata luku väliltä 0-10 ennen vastustajaa.");

            Console.Write("\nMikä on nimesi? ");
            String pelaajanNimi = Console.ReadLine();

            Laheta(palvelin, pep, "JOIN " + pelaajanNimi); // Lähettää palvelimelle, että liitytään peliin

            Boolean on = true;
            String TILA = "JOIN";

            while (on)
            {

                String[] palat = Vastaanota(palvelin); // vastaanottaa viestin palvelimelta. Odottaa niin kauan, että saa viestin

                switch (TILA)
                {
                    case "JOIN":
                        switch (palat[0])
                        {
                            case "ACK":

                                if (palat.Length < 3)
                                {
                                    Console.WriteLine("Palvelimelta saadun merkkijonon muoto on virheellinen: " + String.Join(" ", palat));                                    
                                }

                                switch (palat[1])
                                {
                                    case "201":
                                        Console.WriteLine("Odotetaan toista pelaaja...");
                                        break;
                                    case "202":
                                        Console.WriteLine(palat[2]);
                                        Console.WriteLine("Anna numero");
                                        String luku = Console.ReadLine(); // TODO: palvelin tarkastaa onko hyväksyttävä numero
                                        Laheta(palvelin, pep, "DATA " + luku);
                                        TILA = "GAME";
                                        break;
                                    case "203":
                                        Console.WriteLine(palat[2]);
                                        TILA = "GAME";
                                        break;
                                    default:
                                        Console.WriteLine("Virhe " + String.Join(" ", palat));
                                        break;
                                } // switch (palat[1])
                                break;
                        
                            default:
                                Console.WriteLine("Virhe: " + String.Join(" ", palat));
                                break;

                        } // switch (palat[0])
                        break;

                    case "GAME":

                        switch (palat[0])
                        {                            
                            case "DATA":
                                Console.WriteLine("Vastustajasi arvasi {0}, mutta arvaus ei mennyt oikein.", palat[1]);
                                Console.WriteLine("Paina mitä tahansa näppäintä...");
                                Console.ReadKey();
                                Laheta(palvelin, pep, "ACK 300");

                                Console.Write("\nArvaa luku:\n");
                                String luku = Console.ReadLine();
                                Laheta(palvelin, pep, "DATA " + luku);
                                break;
                            case "QUIT":
                                switch (palat[1])
                                {
                                    case "501":
                                        Console.WriteLine(palat[2]);
                                        TILA = "CLOSE";
                                        break;
                                    case "502":
                                        Console.WriteLine(palat[2]);
                                        TILA = "CLOSE";
                                        break;
                                } // switch(palat[1])

                                break;

                            case "ACK":
                                if (palat[1].Equals("300")) Console.WriteLine(palat[2]);

                                // Tässä virheelliset viestit
                                if (palat[1].Equals("402")) Console.WriteLine(palat[2]);
                                if (palat[1].Equals("403")) Console.WriteLine(palat[2]);

                                break;
                        } // switch(palat[0])

                        break;                   
                } // switch(TILA)

                if (TILA == "CLOSE")
                {
                    Console.WriteLine("Sulje yhteys ja ikkuna painamalla mitä tahansa näppäintä...");
                    Console.ReadKey();
                    Laheta(palvelin, pep, "ACK 500");
                    on = false;
                }

            } // while      

            palvelin.Close();
        }


        /// <summary>
        /// Lähettää viestit palvelimelle 
        /// </summary>
        /// <param name="palvelin">Palvelin jolle lähetetään</param>
        /// <param name="pep">Palvelimen verkko-osoite</param>
        /// <param name="lahetys">Merkkijono joka lähetetään</param>
        public static void Laheta(Socket palvelin, EndPoint pep, String lahetys)
        {
            palvelin.SendTo(Encoding.ASCII.GetBytes(lahetys), pep);
        }


        /// <summary>
        /// Vastaanottaa viestit palvelimelta. Odottaa niin kauan kunnes palvelimelta saapuu viesti.
        /// </summary>
        /// <param name="Palvelin">Palvelin jolta vastaanotetaan</param>
        /// <returns>Taulukko johon vastaanotettu merkkijonosta on paloiteltu erikseen lähettäjä ja viesti</returns>
        public static String[] Vastaanota(Socket palvelin)
        {
            byte[] rec = new byte[256];

            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Palvelinep = (EndPoint)remote;

            String[] palat = new String[5];

            palvelin.ReceiveTimeout = 1000;

            do
            {
                try
                {
                    int received = palvelin.ReceiveFrom(rec, ref Palvelinep);

                    string rec_string = Encoding.ASCII.GetString(rec, 0, received);
                    char[] delim = { ' ' };
                    palat = rec_string.Split(delim, 3);
                }
                catch
                {
                    // timeout
                }
            } while ( String.IsNullOrEmpty(palat[0]));

            return palat;
        }
    }
}
