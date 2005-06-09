// 
// Copyright (c) 2004,2005 Jaroslaw Kowalski <jkowalski@users.sourceforge.net>
// 
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of the Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NLog.Internal
{
    /// <summary>
    /// Sends messages over the network as UDP datagrams.
    /// </summary>
	public class UdpNetworkSender : NetworkSender
	{
        private Socket _socket;
        private Encoding _encoding;
        private IPEndPoint _endpoint;

        /// <summary>
        /// Creates a new instance of <see cref="UdpNetworkSender"/> and initializes
        /// it with the specified URL.
        /// </summary>
        /// <param name="url">URL. Must start with udp://</param>
        public UdpNetworkSender(string url) : base(url)
        {
            // udp://hostname:port

            Uri parsedUri = new Uri(url);
            IPHostEntry host = Dns.GetHostByName(parsedUri.Host);
            int port = parsedUri.Port;

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _endpoint = new IPEndPoint(host.AddressList[0], port);
            _encoding = Encoding.UTF8;
        }

        /// <summary>
        /// Sends the specified text as a UDP datagram.
        /// </summary>
        /// <param name="text"></param>
        protected override void DoSend(string text)
        {
            lock (this)
            {
                _socket.SendTo(_encoding.GetBytes(text), _endpoint);
            }
        }

        /// <summary>
        /// Closes the socket.
        /// </summary>
        public override void Close()
        {
            lock (this)
            {
                try
                {
                    _socket.Close();
                }
                catch (Exception)
                {
                    // ignore errors
                }
                _socket = null;
            }
        }
    }
}