**TCPPacketManager**

A client that connects to ABX Mock Exchange, receives and parses packets, handles missing packets by requesting resend, and saves parsed data into JSON format.

A lightweight TCP client application for connecting to an ABX Mock Exchange server.  
It receives streaming binary packets, parses them, handles missing packet sequences by sending resend requests, and saves the complete and recovered data to a JSON file.

---

## 🚀 Features

- Connect to a TCP server (ABX Exchange)
- Read and parse structured binary packet data
- Detect missing packets based on sequence numbers
- Request resending of specific missing packets
- Merge received and recovered data
- Save all packet information into a JSON file
- Timestamped JSON output for audit and analysis
- Graceful handling of disconnects and reconnections

---

## 📦 Technologies Used

- **C#** (.NET Framework 4.7.2 / .NET 6 / .NET 8 compatible)
- **TCP Networking (System.Net.Sockets)**
- **JSON Serialization (System.Text.Json)**
- --- -----
## 🛠 How It Works

1. Establish TCP connection to the server.
2. Read incoming packets continuously until the server disconnects.
3. Parse fields: Symbol, Indicator, Quantity, Price, Sequence.
4. Identify missing sequences.
5. Send Resend Requests for missing packets (Call Type 2).
6. Re-parse and combine recovered packets.
7. Save all data to a structured JSON file with a timestamp.

---
## 📝 Usage

1. Mention the IP address of the server running pc(port is considerd as 3000)
2. Click on "Stream All Packets"
3. If any packets are missing message will be displayed and click on "Resend Packet"
4. Output will be saved in json file in application path.
