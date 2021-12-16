using CustomSha256;
using CustomSha256.Models.Crypto;

namespace NetworkHost
{
    using System;
    using System.Text;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Collections.Generic;
    public enum Header : byte
    {
        Transaction = 0,
        Block = 1,
        Unknow = 255
    }
    public class DNS
    {
        private List<IPEndPoint> m_IPEndPoints = new List<IPEndPoint>() 
        {
            new IPEndPoint(IPAddress.Loopback, 11000),
            new IPEndPoint(IPAddress.Loopback, 11004),
            new IPEndPoint(IPAddress.Loopback, 11008)
        };

        public List<IPEndPoint> GetListIPEndPoints(IPEndPoint self_IPEndPoint)
        {
            List<IPEndPoint> list = new List<IPEndPoint>(m_IPEndPoints);
            list.Remove(self_IPEndPoint);
            return list;
        }
    }
    public class Host
    {

        private IPAddress m_address;
        private int m_port;
        private IPEndPoint m_endpoint_listen;
        private TcpListener m_listener;
        private bool m_is_listening = false;
        private DNS m_dns = new DNS();

        public delegate void MethodAcceptData(Header header, byte[] data);
        public event MethodAcceptData dataReceived;
        public Host(IPAddress addres, int port)
        {
            m_address = addres;
            m_port = port;
            m_endpoint_listen = new IPEndPoint(m_address, m_port);
            m_listener = new TcpListener(m_endpoint_listen);
        }

        public void startListening()
        {
            m_is_listening = true;
            m_listener.Start();
            Console.WriteLine("listening start: " + m_address + " " + m_port);

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

                    parseGotData(data);
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
        public void sendTransaction(byte[] transaction)
        {
            List<IPEndPoint> endpoints = m_dns.GetListIPEndPoints(m_endpoint_listen);
            foreach(IPEndPoint endpoint in endpoints)
            {
                sendTransaction(endpoint.Address, endpoint.Port, transaction);
            }
        }
        public void sendBlock(IPAddress address, int port, byte[] block)
        {
            sendDataWithHeader(address, port, Header.Block, block);
        }
        public void sendBlock(byte[] block)
        {
            List<IPEndPoint> endpoints = m_dns.GetListIPEndPoints(m_endpoint_listen);
            foreach (IPEndPoint endpoint in endpoints)
            {
                sendBlock(endpoint.Address, endpoint.Port, block);
            }
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
        private void parseGotData(byte[] data)
        {
            switch ((Header)data[0])
            {
                case Header.Transaction:
                    {
                        byte[] transaction = new byte[data.Length - 1];
                        Array.Copy(data, 1, transaction, 0, data.Length - 1);
                        dataReceived(Header.Transaction, transaction);
                        break;
                    }
                case Header.Block:
                    {
                        byte[] block = new byte[data.Length - 1];
                        Array.Copy(data, 1, block, 0, data.Length - 1);
                        dataReceived(Header.Block, block);
                        break;
                    }
                default: 
                    {
                        byte[] unknow = new byte[data.Length - 1];
                        Array.Copy(data, 1, unknow, 0, data.Length - 1);
                        dataReceived(Header.Unknow, unknow);
                        break;
                    }
            }
        }
    }
}
