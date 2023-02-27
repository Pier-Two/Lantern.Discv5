using System.Net;
using System.Net.Sockets;

namespace Lantern.Discv5.WireProtocol
{
    public class UdpCommunication
    {
        private const int MaxPacketSize = 1280;
        private const int MinPacketSize = 63;
        private const int TimeoutMilliseconds = 500;

        private readonly UdpClient _udpClient;

        public UdpCommunication(int port)
        {
            _udpClient = new UdpClient(port);
        }
        
        public async Task SendAsync(byte[] data, IPEndPoint destination)
        {
            ValidatePacketSize(data);
            var sendTask = _udpClient.SendAsync(data, data.Length, destination);
            var timeoutTask = Task.Delay(TimeoutMilliseconds);
            var completedTask = await Task.WhenAny(sendTask, Task.Delay(TimeoutMilliseconds));

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Send timed out");
            }
        }

        public async Task<byte[]> ReceiveAsync()
        {
            var receiveResult = await ReceiveAsyncWithTimeoutAsync();
            ValidatePacketSize(receiveResult.Buffer);

            return receiveResult.Buffer;
        }
        
        public void Close()
        {
            _udpClient.Close();
        }
        
        private static void ValidatePacketSize(byte[] data)
        {
            if (data.Length < MinPacketSize)
            {
                throw new InvalidDataException("Packet is too small");
            }
            if (data.Length > MaxPacketSize)
            {
                throw new InvalidDataException("Packet is too large");
            }
        }

        private async Task<UdpReceiveResult> ReceiveAsyncWithTimeoutAsync()
        {
            var receiveTask = _udpClient.ReceiveAsync();
            var timeoutTask = Task.Delay(TimeoutMilliseconds);
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Receive timed out");
            }

            return receiveTask.Result;
        }
        
    }
}
