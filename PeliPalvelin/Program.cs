using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace PeliPalvelin
{
    class Program
    {
        // enum Field { Viesti, Data };
        // static String erotin = " ";

        static void Main(string[] args)
        {
            Socket palvelin;
            IPEndPoint iep = new IPEndPoint(IPAddress.Loopback, 9999);
            
            try
            {
                palvelin = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                palvelin.Bind(iep);
            }
            catch (Exception e)
            {
                Console.WriteLine("Virhe: " + e.Message);
                Console.ReadKey();
                return;
            }

            String STATE = "WAIT";            
            Console.WriteLine("Tila: WAIT");

            Boolean on = true;
            int vuoro = -1;
            int Pelaajat = 0;
            int Quit_ACK = 0;
            int luku = -1;
            EndPoint[] Pelaaja = new EndPoint[2];
            String[] Nimi = new string[2];

            while (on)
            {
                IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remote = (EndPoint)client;
                String[] kehys = Vastaanota(palvelin, ref remote);
              //Console.WriteLine(String.Join(" ", kehys) + " tämä viesti vastaantotettiin");

                switch (STATE)
                {
                    case "WAIT":
                        switch (kehys[0])
                        {
                            case "JOIN":
                                Pelaaja[Pelaajat] = remote;
                                Nimi[Pelaajat] = kehys[1];
                                Pelaajat++;

                                if (Pelaajat == 1)
                                {
                                    Laheta(palvelin, Pelaaja[0], "ACK 201 JOIN OK");
                                }
                                else if (Pelaajat == 2)
                                {
                                    // Arvotaan aloittaja
                                    // Arvotaan oikea luku 
                                    Random rand = new Random();
                                    int Aloittaja = rand.Next(0, 2);
                                    vuoro = Aloittaja;
                                    luku = rand.Next(0, 10);
                                    Console.WriteLine("Aloittaja: " + Nimi[Aloittaja]);
                                    Console.WriteLine("Arvattava luku: " + luku);

                                    Laheta(palvelin, Pelaaja[Aloittaja], "ACK 202 Vastustajasi nimi on " + Nimi[Flip(Aloittaja)] + ", 2 pelaajaa liittynyt, saat aloittaa");
                                    Laheta(palvelin, Pelaaja[Flip(Aloittaja)], "ACK 203 Vastustajasi nimi on " + Nimi[Aloittaja] + ", 2 pelaajaa liittynyt, vastustajasi aloittaa");
                                    
                                    STATE = "GAME";
                                    Console.WriteLine("TILA: GAME");
                                }
                                                               
                                break;

                        } //switch(kehys[0])
                        break;

                    case "GAME":
                      //Console.WriteLine("Päästiin game tilaan");
                        switch (kehys[0])
                        {
                            case "DATA":                                
                                if (remote.Equals(Pelaaja[vuoro]))
                                {                               
                                    if (int.Parse(kehys[1]) == luku) {
                                        Laheta(palvelin, Pelaaja[vuoro], "QUIT 501 Arvasit oikein! Luku oli siis " + luku);
                                        Laheta(palvelin, Pelaaja[Flip(vuoro)], "QUIT 502 " + Nimi[vuoro] + " arvasi oikein. " + "Luku oli " + luku);

                                        STATE = "END";
                                        Console.WriteLine("TILA: END");

                                        break;
                                    }

                                    Laheta(palvelin, Pelaaja[vuoro], "ACK 300 DATA OK");
                                    vuoro = Flip(vuoro);
                                    Laheta(palvelin, Pelaaja[vuoro], "DATA " + kehys[1]);

                                    STATE = "WAIT_ACK";
                                    Console.WriteLine("TILA: WAIT_ACK");

                                } else if (remote.Equals(Pelaaja[Flip(vuoro)])) {
                                    Laheta(palvelin, Pelaaja[Flip(vuoro)], "ACK 402 Vastustajan vuoro.");
                                }

                                break;
                        }
                        
                        break;

                    case "WAIT_ACK":
                        switch (kehys[0])
                        {
                            case "ACK":
                                if (kehys[1].Equals("300"))
                                {
                                    STATE = "GAME";
                                    Console.WriteLine("TILA: GAME");
                                }
                                else Laheta(palvelin, Pelaaja[vuoro], "ACK 403 ACK viesti on virheellinen. DATA kuitataan vastaanotetuksi kirjoittamalla ACK 300"); ;                             
                                break;
                        }                          
                        
                        break;
                    case "END":
                        //Quit_ACK
                        switch (kehys[0])
                        {
                            case "ACK":
                                if (kehys[1].Equals("500")) Quit_ACK++;
                                if (Quit_ACK == 2) on = false;
                                break;
                        }

                        break;

                    default:
                        Console.WriteLine("Errors... ");
                        break;
                }
            }
            // TILA = "CLOSED" 

            palvelin.Close();
        }


        /// <summary>
        /// Vastaanottaa asiakkaiden lähettämät viestit
        /// </summary>
        /// <param name="palvelin">palvelin jolle lähetetään</param>
        /// <returns>Välilyöntien mukaan paloiteltu viesti</returns>
        public static String[] Vastaanota(Socket palvelin, ref EndPoint remote)
        {
            byte[] rec = new byte[256];
           
            String[] palat = new String[5];

            palvelin.ReceiveTimeout = 1000;

 //         do {
                try
                {
                    int received = palvelin.ReceiveFrom(rec, ref remote);

                    string rec_string = Encoding.ASCII.GetString(rec, 0, received);
                    char[] delim = { ' ' };
                    palat = rec_string.Split(delim, 3);
                    Console.WriteLine("Viesti vastaan otettu: " + remote);
                }
                catch
                {
                    // timeout
                }
 //         } while (String.IsNullOrEmpty(palat[0]));

            return palat;
        }


        /// <summary>
        /// Lähettää viestin pelaajalle
        /// </summary>
        /// <param name="palvelin">Palvelin joka lähettää</param>
        /// <param name="pelaaja">Pelaaja jolle lähetetään</param>
        /// <param name="viesti">Lähetettävä viesti</param>
        public static void Laheta(Socket palvelin, EndPoint pelaaja, string viesti)
        {
            try
            {
                palvelin.SendTo(Encoding.ASCII.GetBytes(viesti), pelaaja);
            }
            catch (Exception e)
            {
                Console.WriteLine("Poikkeus: " + e.Message);
            }
        }
        

        /// <summary>
        /// Vaihdetaan vuorossa olevaa pelaajaa
        /// </summary>
        /// <param name="vaihdettava">Pelaajan indeksi vaidetaan vastakkaiseksi (2 pelaajaa: 0 -> 1 tai 1 -> 0)</param>
        /// <returns></returns>
        public static int Flip(int vaihdettava)
        {
            return 1 - vaihdettava;
        }
    }
}
