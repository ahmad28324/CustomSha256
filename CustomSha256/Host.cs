using CustomSha256;
using CustomSha256.Models.Crypto;

namespace NetworkHost
{
    using System;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public enum Header : byte
    {
        Transaction = 0,
        Block = 1,
        Unknow = 255
    }
    public class Host
    {

        private IPAddress m_address;
        private int m_port;
        private IPEndPoint m_endpoint_listen;
        private TcpListener m_listener;
        private bool m_is_listening = false;
        public Host(IPAddress addres, int port)
        {
            m_address = addres;
            m_port = port;
            m_listener = new TcpListener(new IPEndPoint(m_address, m_port));
        }

        public void startListening()
        {
            m_is_listening = true;
            m_listener.Start();
            Console.WriteLine("listening start: " + m_address + " " + m_port);

            // в отдельный поток, или спец. функция. ConceletionToken
            while (m_is_listening)
            {
                if (m_listener.Pending())
                {
                    TcpClient client = m_listener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int num_real_bytes = stream.Read(buffer, 0, buffer.Length);
                    client.Close();

                    byte[] data = new byte[num_real_bytes];
                    Array.Copy(buffer, data, data.Length);

                    object obj = parseGotData(data);
                }
            }
            m_is_listening = false;
        }
        public void startListeningInThread()
        {
            m_is_listening = true;
            Thread thread = new Thread(new ThreadStart(startListening));
            thread.Start();
        }
        public void stopListening()
        {
            m_is_listening = false;
        }
        public void sendMessage(IPAddress address, int port, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            sendData(address, port, data);
        }
        public void sendTransaction(IPAddress address, int port, byte[] transaction)
        {
            sendDataWithHeader(address, port, Header.Transaction, transaction);
        }
        public void sendBlock(IPAddress address, int port, byte[] block)
        {
            sendDataWithHeader(address, port, Header.Block, block);
        }
        private void sendDataWithHeader(IPAddress address, int port, Header header, byte[] p_data)
        {
            byte[] data = new byte[p_data.Length + 1];
            data[0] = (byte)header;
            Array.Copy(p_data, 0, data, 1, p_data.Length);
            sendData(address, port, data);
        }
        private void sendData(IPAddress address, int port, byte[] data)
        {
            IPEndPoint remove_host = new IPEndPoint(address, port);
            try
            {
                TcpClient m_client = new TcpClient();
                m_client.Connect(remove_host);

                NetworkStream stream = m_client.GetStream();
                stream.Write(data);
                m_client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private object parseGotData(byte[] data)
        {
            //string message = Encoding.UTF8.GetString(data);
            //Console.WriteLine(message);

            switch ((Header)data[0])
            {
                case Header.Transaction:
                    {
                        Console.WriteLine("TRANSACTION");
                        Transaction transaction = (Transaction)Utils.ByteArrayToObject(data);
                        return transaction;
                    }
                case Header.Block:
                    {
                        Console.WriteLine("BLOCK");
                        Block block = (Block)Utils.ByteArrayToObject(data);
                        return block;
                    }
                default: 
                    {
                        Console.WriteLine(data[0]);
                        Console.WriteLine(Encoding.UTF8.GetString(data, 1, data.Length - 1));
                        return null;
                    }
            }
        }
    }
}
