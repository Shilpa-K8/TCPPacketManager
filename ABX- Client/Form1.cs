using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ABX__Client;

namespace ABX__Client
{
    public partial class Form1 : Form
    {
        const int PacketSize = 17;
        public Form1()
        {
            InitializeComponent();
        }
        List<int> sequences = new List<int>();
        List<int> missing = new List<int>();
        // List<Packet> packets = new List<Packet>();
        List<Packet> receivedPackets = new List<Packet>();
        List<Packet> recoveredPackets = new List<Packet>();

        private void button1_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            try
            {
                string serverIp = textBox2.Text; // Replace with actual ABX IP or hostname
                int port = 3000;
                using (TcpClient client = new TcpClient(serverIp, port))
                using (NetworkStream stream = client.GetStream())
                {
                    stream.ReadTimeout = 5000;
                    byte[] request = new byte[2];
                    request[0] = 1;  // Call Type: Stream All Packets
                    request[1] = 0;  // ResendSeq: not used here
                    stream.Write(request, 0, request.Length);
                    Logs.statuslog_write("Request sent to server.");

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        // Read until the server closes the connection
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        try
                        {
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                memoryStream.Write(buffer, 0, bytesRead);
                            }
                        }
                        catch (IOException ex)
                        {
                            Logs.errorlog_write("Read completed or timed out: " + ex.Message);
                        }

                        byte[] allData = memoryStream.ToArray();
                        int totalPackets = allData.Length / PacketSize;


                        for (int i = 0; i < totalPackets; i++)
                        {
                            int offset = i * PacketSize;

                            string symbol = Encoding.ASCII.GetString(allData, offset, 4);
                            char indicator = (char)allData[offset + 4];
                            int quantity = ReadInt32BigEndian(allData, offset + 5);
                            int price = ReadInt32BigEndian(allData, offset + 9);
                            int sequence = ReadInt32BigEndian(allData, offset + 13);

                            sequences.Add(sequence);

                            Packet packet = new Packet
                            {
                                Symbol = symbol,
                                Indicator = indicator,
                                Quantity = quantity,
                                Price = price,
                                Sequence = sequence
                            };

                            receivedPackets.Add(packet);



                            Logs.statuslog_write($"[{i + 1}] Symbol: {symbol}, Indicator: {indicator}, Quantity: {quantity}, Price: {price}, Seq: {sequence}");
                        }

                        Logs.statuslog_write($"✅ Parsed {totalPackets} packets.");
                    }

                    MissedSequence(sequences);


                }



            }
            catch (Exception ex)
            {

                Logs.errorlog_write("❌ Error: " + ex.Message);
            }


        }

        static int ReadInt32BigEndian(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) |
                   (buffer[offset + 1] << 16) |
                   (buffer[offset + 2] << 8) |
                   buffer[offset + 3];
        }
        static StockPacket ParsePacket(byte[] packetData)
        {
            // Extract fields from the binary packet (Big Endian)
            string symbol = Encoding.ASCII.GetString(packetData, 0, 4).Trim();
            char buySellIndicator = (char)packetData[4];
            int quantity = BitConverter.ToInt32(new byte[] { packetData[8], packetData[9], packetData[10], packetData[11] }, 0);
            int price = BitConverter.ToInt32(new byte[] { packetData[12], packetData[13], packetData[14], packetData[15] }, 0);
            int sequence = BitConverter.ToInt32(new byte[] { packetData[16], packetData[17], packetData[18], packetData[19] }, 0);

            // Ensure we handle Big Endian byte order correctly
            return new StockPacket
            {
                Symbol = symbol,
                BuySellIndicator = buySellIndicator,
                Quantity = quantity,
                Price = price,
                Sequence = sequence
            };
        }

        // ✅ Identify missing sequence numbers
        private List<int> MissedSequence(List<int> sq)
        {
            Logs.statuslog_write("\n🔍 Checking for missing sequences...");
            sequences.Sort();
            int minSeq = sequences[0];
            int maxSeq = sequences[^1];

            for (int seq = minSeq; seq <= maxSeq; seq++)
            {
                if (!sequences.Contains(seq))
                {
                    missing.Add(seq);
                }
            }

            if (missing.Count == 0)
            {
                Logs.statuslog_write("✅ No missing sequences detected.");
                SaveToJson.Save(receivedPackets, recoveredPackets, "packets_data.json");
                Application.Exit();
            }

            else
            {
                Logs.statuslog_write($"⚠️ Missing sequence numbers: {string.Join(", ", missing)}");
                MessageBox.Show("Some packets are missing please click on Resend Packet");
            }
            return missing;

        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                string serverIp = textBox2.Text; // Replace with actual ABX IP or hostname
                int port = 3000;
                using (TcpClient client = new TcpClient(serverIp, port))
                using (NetworkStream stream = client.GetStream())
                {

                    stream.ReadTimeout = 5000;
                    // Step 4: Resend requests for missing packets
                    foreach (int seq in missing)
                    {


                        byte[] resendRequest = new byte[2];
                        resendRequest[0] = 2;           // Call Type 2: Resend Packet
                        resendRequest[1] = (byte)seq;  // ResendSeq: Missing sequence number


                        try
                        {
                            stream.Write(resendRequest, 0, resendRequest.Length);
                            stream.Flush();

                            Logs.statuslog_write($"📤 Resend request sent for packet: {seq}");
                        }
                        catch (Exception ex)
                        {
                            Logs.errorlog_write("❌ Failed to send resend request: " + ex.Message);
                            continue;
                        }

                        try
                        {
                            // Wait to receive exactly 1 packet
                            byte[] packetBuffer = new byte[PacketSize];
                            int totalRead = 0;

                            while (totalRead < PacketSize)
                            {
                                int read = stream.Read(packetBuffer, totalRead, PacketSize - totalRead);
                                if (read == 0)
                                {
                                    // Server closed connection unexpectedly
                                    Logs.statuslog_write("⚠️ Connection closed while reading packet.");
                                    break;
                                }
                                totalRead += read;
                            }

                            if (totalRead == PacketSize)
                            {
                                string symbol = Encoding.ASCII.GetString(packetBuffer, 0, 4);
                                char indicator = (char)packetBuffer[4];
                                int quantity = ReadInt32BigEndian(packetBuffer, 5);
                                int price = ReadInt32BigEndian(packetBuffer, 9);
                                int sequence = ReadInt32BigEndian(packetBuffer, 13);

                                sequences.Add(sequence);
                                Packet packet = new Packet
                                {
                                    Symbol = symbol,
                                    Indicator = indicator,
                                    Quantity = quantity,
                                    Price = price,
                                    Sequence = sequence
                                };

                                recoveredPackets.Add(packet);

                                Logs.statuslog_write($"✅ Resent Packet: Symbol: {symbol}, Indicator: {indicator}, Quantity: {quantity}, Price: {price}, Seq: {sequence}");
                            }
                            else
                            {
                                Logs.statuslog_write($"❌ Incomplete packet received for seq {seq}. Bytes read: {totalRead}");
                            }
                        }
                        catch (IOException ex)
                        {
                            Logs.errorlog_write("Read error during resend: " + ex.Message);
                        } 

                    }
                    // Close the connection when done
                    client.Close();

                    //save output in Json
                    SaveToJson.Save(receivedPackets, recoveredPackets, "packets_data.json");


                }
             
            }

            catch (Exception ex)
            {

                Logs.errorlog_write("❌ Error: " + ex.Message);
            }



        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = false;
        }
    }

    class StockPacket
    {
        public string Symbol { get; set; }
        public char BuySellIndicator { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Sequence { get; set; }
    }


    public class Packet
    {
        public string Symbol { get; set; }
        public char Indicator { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Sequence { get; set; }
    }


    public static class SaveToJson
    {
        public static void Save(
            List<Packet> firstTimePackets,
            List<Packet> recoveredPackets,
            string filePath)
        {
            var output = new
            {
                SavedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),  // UTC timestamp in ISO format
                ReceivedPackets = firstTimePackets.OrderBy(p => p.Sequence).ToList(),
                RecoveredPackets = recoveredPackets.OrderBy(p => p.Sequence).ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonString = JsonSerializer.Serialize(output, options);
            File.WriteAllText(filePath, jsonString);

            Logs.statuslog_write($"✅ JSON saved with timestamp at {filePath}");
            MessageBox.Show($"Output file saved at {filePath}");
        }
    }

}


