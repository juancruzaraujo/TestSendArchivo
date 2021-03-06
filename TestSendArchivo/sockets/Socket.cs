﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Sockets
{
    public class Sockets
    {

        //variables y objetos privados
        //private Client _objClient;
        private List<Client> _lstObjClient;
        private List<ServerTCP> _lstObjServerTCP;
        private ServerUDP _objServerUDP;

        private string _message;

        private bool _serverMode;
        private bool _clientMode;


        private int _serverPortListening;
        private int _codePage;
        private int _connectionNumber;
        private int _clientConnectionNumberTCP;
        private int _clientConnectionNumberUDP;
        private bool _listening;
        private int _clientPort;
        private long _size;
        private string _ipClient;
        private string _data;
        private string _host;
        private bool _tcp;
        private int _maxServerConnectionNumber;
        private int _numberServerConnections;
        private int _numCliConServer;
        private bool _serverListening;
        private bool _stopingServer;
        private bool _serverStarted;
        private int _serverReceiveTimeout;

        //constantes priavdas
        private const string C_MENSAJE_ERROR_MODO_SOY_CLIENTE = "modo cliente";
        private const string C_MENSAJE_ERROR_MODO_SOY_SERVER = "modo server";

        public const int C_DEFALT_CODEPAGE = 65001;

        class SendArrayParamsContainer
        {
            internal byte[] memArray;
            internal int clusterSize;
            //internal int indexConection;
            internal int connectionNumber;
        }

        public delegate void Delegate_Socket_Event(EventParameters eventParameters);
        public event Delegate_Socket_Event Event_Socket;
        public void EventSocket(EventParameters parameters)
        {
            this.Event_Socket(parameters);
        }


        public int GetMaxServerConexiones
        {
            get
            {
                return _maxServerConnectionNumber;
            }
        }

        public bool tcp
        {
            get
            {
                return _tcp;
            }
        }

        /*
        public string IpConnectedClient
        {
            get
            {
                string res = "";
                if (_serverMode)
                {
                    //res = _objServidor.ip_Conexion;
                }
                return res;
            }
        }*/

        public int MaxNumberServerConnections
        {
            set
            {
                _maxServerConnectionNumber = value;
                if (_serverMode)
                {
                    if (!_tcp)
                    {
                        _objServerUDP.MaxClientUDP = _maxServerConnectionNumber;
                    }
                }
            }
            get
            {
                return _maxServerConnectionNumber;
            }
        }

        public Sockets()
        {

        }

        void Error(string errorMessage)
        {
            EventParameters ev = new EventParameters();

            ev.SetData(errorMessage)
                .SetEvent(EventParameters.EventType.ERROR)
                .SetConnectionNumber(_connectionNumber).SetListening(_listening)
                .SetSize(_size)
                .SetClientIp(_ipClient);

            EventSocket(ev);
        }

     
        public void ConnectClient(int port,string host,int timeOut = 30,bool tcp = true,int codePage = C_DEFALT_CODEPAGE,int receiveTimeout = 0)
        {

            int connNumber;

            if (_lstObjClient == null)
            {
                _lstObjClient = new List<Client>();
            }

            _clientPort = port;
            _codePage = codePage;
            //_connectionNumber = connectionNumber;
            _host = host;

            Client objClient = new Client(tcp);

            //objClient = new Client(tcp);
            objClient.SetGetTimeOut = timeOut;
            objClient.ReceiveTimeout = receiveTimeout;
            objClient.CodePage(_codePage);
            

            objClient.clientEvent += new Client.Delegated_Client_Event(Evsocket);
            
            _clientMode = true;

            _lstObjClient.Add(objClient);

            if (tcp)
            {
                _clientConnectionNumberTCP++;
                connNumber = _clientConnectionNumberTCP;
            }
            else
            {
                _clientConnectionNumberUDP++;
                connNumber = _clientConnectionNumberUDP;
            }
            _lstObjClient[_lstObjClient.Count()-1].Connect(connNumber,host, port);
        }



        /// <summary>
        /// set the server parameters
        /// </summary>
        /// <param name="port">listening port</param>
        /// <param name="codePage">codepage communication</param>
        /// <param name="tcp">protocol, tpc = true is default value</param>
        /// <param name="maxCon">maximum number of connections, in udp mode 0 is for unlimited connections</param>
        /// <param name="receiveTimeout">(only tcp mode) maximum waiting time in seconds for an incoming message, if it is 0 the waiting time is unlimited</param>
        public void SetServer(int port, int codePage = C_DEFALT_CODEPAGE, bool tcp = true, int maxCon = 0,int receiveTimeout=0)
        {
            string res = "";
            _listening = false;
            _serverPortListening = port;
            _codePage = codePage;
            _tcp = tcp;
            _maxServerConnectionNumber = maxCon;
            _numberServerConnections = 0;
            _numCliConServer = 1;
            _serverReceiveTimeout = receiveTimeout;
            this.ServerMode = true;


            if (!ServerMode) //ya esta activado de antes el modo cliente
            {
                Error(ServerMode.ToString());
                return;
            }
            if (_tcp)
            {
                _lstObjServerTCP = new List<ServerTCP>();
            }

            CreateServer(ref res);

            if (res != "")
            {
                _message = res;
                Error(res);
                return;
            }
            _listening = true;
            _serverMode = true;
        }

        private void CreateServer(ref string message)
        {
            int indexList = GetLastSpaceFree();

            if (_tcp)
            {
                ServerTCP objServidor = new ServerTCP(_serverPortListening, _codePage, ref message);
                if (message != "")
                {
                    Error(message);
                    return;
                }
                objServidor.evento_servidor += new ServerTCP.Delegate_Server_Event(Evsocket);
                _lstObjServerTCP.Add(objServidor);

                _lstObjServerTCP[indexList].IndexConnection = _numCliConServer;
                _lstObjServerTCP[indexList].ListIndex = indexList;
                _lstObjServerTCP[indexList].ReceiveTimeout = _serverReceiveTimeout;
            }
            else
            {
                _objServerUDP = new ServerUDP(_serverPortListening);
                _objServerUDP.MaxClientUDP = _maxServerConnectionNumber;
                _objServerUDP.CodePage(_codePage);
                if (message != "")
                {
                    Error(message);
                    return;
                }
                _objServerUDP.evento_servidor += new ServerUDP.Delegate_Server_Event(Evsocket);

            }

        }

        /// <summary>
        /// start the server
        /// </summary>
        public void StartServer()
        {
            string res = "";
            if (_tcp)
            {
                _lstObjServerTCP[_lstObjServerTCP.Count() - 1].Start(ref res);

                if (res != "")
                {
                    Error(res);
                    return;
                }

                _serverListening = true;
            }
            else
            {
                _objServerUDP.Start();
            }

            if (!_serverStarted)
            {
                _serverStarted = true;
                EventParameters ev = new EventParameters();
                ev.SetEvent(EventParameters.EventType.SERVER_START);
                Evsocket(ev);
            }

        }

        #region propiedades
        public bool ServerMode
        {
            get
            {
                return _serverMode;
            }
            set
            {
                _serverMode = value;

            }
        }

        public bool ClientMode
        {
            get
            {
                return _clientMode;
            }
            set
            {
                _clientMode = value;
            }
        }

        public int PortListeninServer
        {
            get
            {
                return _serverPortListening;
            }
            set
            {
                _serverPortListening = value;
            }
        }

        public int CodePage
        {
            get
            {
                return _codePage;
            }
            set
            {
                _codePage = value;
            }
        }

        public int ConnectionNumber
        {
            get
            {
                return _connectionNumber;
            }
            set
            {
                _connectionNumber = value;
            }
        }

        public string Message
        {
            get
            {
                return _message;
            }
        }

        public int GetConnectionNumber()
        {
            return _numberServerConnections;
        }

        public int GetUltimoNumeroClienteConectado
        {
            get
            {
                return _numCliConServer;
            }
        }


        public string Host
        {
            get
            {
                return _host;
            }
            set
            {
                _host = value;
            }
        }

        /// <summary>
        /// reciebe timeout in secons. If the value is 0 there is no timeout
        /// </summary>
        public int ReceiveTimeout
        {
            set
            {
                if (ServerMode)
                {

                }
                else
                {

                }
            }
        }

        public void Disconnect(int connectionNumber)
        {
            if (ClientMode)
            {
                int index = GetClientListIndex(connectionNumber);
                _lstObjClient[index].CloseConnection();
            }
        }

        public void DisconnectAll()
        {
            for (int i =0;i<_lstObjClient.Count();i++)
            {
                _lstObjClient[i].CloseConnection();
            }
        }

        /// <summary>
        /// disconnect a client from the server
        /// </summary>
        /// <param name="connectionNumber">Cliente connection number</param>
        public void DisconnectConnectedClientToMe(int connectionNumber)
        {
            if (ServerMode)
            {
                if (_tcp)
                {
                    int clientIndexDisconnect = GetListIndexConnectedClientToServer(connectionNumber);
                    if (clientIndexDisconnect >= 0)
                    {
                        _lstObjServerTCP[clientIndexDisconnect].DisconnectClient();
                        OrderClientList();
                    }

                }
                else
                {
                    _objServerUDP.Disconnect(connectionNumber);
                }
            }
        }

        public void DisconnectAllConnectedClientsToMe()
        {
            if (_tcp)
            {

                //for (int i = _lstObjServerTCP.Count() -1; i >= 0; i--)
                for (int i = 0; i < _lstObjServerTCP.Count(); i++)
                {
                    bool lastClientConnected = false;
                    if (i == _lstObjServerTCP.Count() -1)
                    {
                        lastClientConnected = true;
                    }

                    _lstObjServerTCP[i].DisconnectClient(_stopingServer,lastClientConnected);

                    OrderClientList();

                }

            }
            else
            {
                _objServerUDP.DisconnectAll();
            }
        }

        public void KillServer()
        {
            if (_serverMode)
            {
                _serverListening = false;
                _stopingServer = true;
                if (tcp)
                {
                    DisconnectAllConnectedClientsToMe();
                    //_lstObjServerTCP.Clear();
                }
                else
                {
                    _objServerUDP.StopServer();
                }
            }
        }

        public bool ClientConnected(int connectionNumber)
        {

            return _lstObjClient[GetClientListIndex(connectionNumber)].conected;
            
        }
        #endregion

        //TODOS LOS EVENTOS DEL SERVIDOR Y CLIENTES, PRIMERO PASAN POR ACÁ
        private void Evsocket(EventParameters ev)
        {
            
            string message = "";
            bool showEvMaxConnections = false;

            if ((_ipClient != ""))
            {
                if (_ipClient != ev.GetClientIp)
                {
                    _ipClient = ev.GetClientIp;
                }
            }

            
            switch (ev.GetEventType)
            {
                /*case EventParameters.EventType.ERROR:
                    if (!_tcp)
                    {
                        //string aux = "";
                        //_objServidor.Start(ref aux); //volvemos a iniciar el servidor udp
                    }
                    break;
                */

                case EventParameters.EventType.NEW_CONNECTION:

                    if (_serverMode)
                    {
                        if (!_tcp)
                        {
                            //
                        }
                        else
                        {
                            if (_numberServerConnections >= (_maxServerConnectionNumber - 1)) //muy cabeza, pero funciona
                            {
                                showEvMaxConnections = true; //genero el evento de limite de conexiones
                            }
                            _numberServerConnections++;
                            _numCliConServer++;
                            if (GetConnectionNumber() < _maxServerConnectionNumber)
                            {
                                CreateServer(ref message);
                                StartServer();
                            }
                        }
                    }
                        
                    break;

                case EventParameters.EventType.END_CONNECTION: //UDP no dispara este evento
                        
                    if (_serverMode)
                    {
                        _lstObjServerTCP.RemoveAt(ev.GetListIndex);
                        OrderClientList();
                        _numberServerConnections--;

                        if (!_serverListening && !_stopingServer)
                        {
                            CreateServer(ref message);
                            StartServer();
                        }
                    }
                    else
                    {
                        int cliIndex; //= GetClientListIndex(ev.GetConnectionNumber);
                        //if (ev.GetTCP)
                        {
                            cliIndex = GetClientListIndex(ev.GetConnectionNumber);
                            _lstObjClient.RemoveAt(cliIndex);
                        }
                        //else
                        //{

                        //}

                    }
                        

                    break;

                case EventParameters.EventType.SERVER_STOP:
                    _stopingServer = false;
                    _serverStarted = false;
                    if (tcp)
                    {
                        _lstObjServerTCP.Clear();
                    }
                    break;
            }

            EventSocket(ev); //envío el evento a quien lo este consumiendo(?)

            //pongo esto acá ya que tengo que ser lo último que muestro
            if (showEvMaxConnections)
            {
                showEvMaxConnections = false;
                _serverListening = false;
                EventParameters evMaxCon = new EventParameters();
                evMaxCon.SetEvent(EventParameters.EventType.CONNECTION_LIMIT);
                EventSocket(evMaxCon);
            }
        }



        public void Send(int connectionNumber,string message)
        {
            if (_serverMode)
            {
                if (_tcp)
                {
                    _lstObjServerTCP[GetListIndexConnectedClientToServer(connectionNumber)].Send(message);
                }
                else
                {
                    _objServerUDP.Send(connectionNumber, message);
                }
            }
            else
            {
                _lstObjClient[GetClientListIndex(connectionNumber)].Send(message);
            }
        }

        /// <summary>
        /// envía un message al cliente o al servidor. si hay un error se dispara un evento de error
        /// </summary>
        /// <param name="message">message a enviar</param>
        public void Send(string message, int index = 0)
        {

            if (_serverMode)
            {
                if (_tcp)
                {
                    if (_lstObjServerTCP[index].Connected)
                    {
                        _lstObjServerTCP[index].Send(message); //saczar ref res
                    }
                }
                else
                {
                    _objServerUDP.Send(message, index);
                }
            }

            if (_clientMode)
            {
                _lstObjClient[index].Send(message);
            }
        }

        /// <summary>
        /// send a message to all clientes connected
        /// </summary>
        /// <param name="message"></param>
        public void SendAll(string message)
        {
            try
            {
                if (_serverMode)
                {
                    if (_tcp)
                    {
                        for (int i = 0; i < _lstObjServerTCP.Count(); i++)
                        {
                            Send(message, i);
                        }
                    }
                    else
                    {
                        _objServerUDP.SendAll(message);
                    }
                }
                else
                {
                    for (int i =0;i<_lstObjClient.Count();i++)
                    {
                        Send(message, i);
                    }
                }
            }
            catch (Exception err)
            {
                GenerateErrorEvent(err);
            }

        }


        public void SendArray(byte[] memArray, int clusterSize, int connectionNumber)
        {
            try
            {

                SendArrayParamsContainer sendArrayParams = new SendArrayParamsContainer();
                sendArrayParams.memArray = memArray;
                sendArrayParams.clusterSize = clusterSize;
                sendArrayParams.connectionNumber = connectionNumber;

                Thread sendArrayThread = new Thread(new ParameterizedThreadStart(SendArrayThread));
                sendArrayThread.Name = "sendArrayThread_" + connectionNumber.ToString();
                sendArrayThread.Start(sendArrayParams);

            }
            catch (Exception err)
            {
                GenerateErrorEvent(err);
            }
        }

        public String GetClientHost(int ConnectionNumber)
        {
            return _lstObjClient[GetClientListIndex(ConnectionNumber)].GetHost;
        }
        public int GetClientPort(int ConnectionNumber)
        {
            return _lstObjClient[GetClientListIndex(ConnectionNumber)].GetPort;
        }

        public int GetClientListIndex(int connectionNumber)
        {
            for (int i =0; i<_lstObjClient.Count();i++)
            {
                if (connectionNumber == _lstObjClient[i].GetConnectionNumber)
                {
                    return i;
                }
            }

            return -1;
        }

        private void SendArrayThread(object sendArrayParams)
        {
            string data = "";
            int actualPositionNumber = 0;
            int nSize;
            int resultNumber = 0;
            //int nPosLectura = 0;
            int readingPositionNumber = 0;
            int conditionNumber;

            SendArrayParamsContainer aux = (SendArrayParamsContainer)sendArrayParams;
            SendArrayParamsContainer sendParams = new SendArrayParamsContainer();
            sendParams.clusterSize = aux.clusterSize;
            sendParams.connectionNumber = aux.connectionNumber;
            sendParams.memArray = aux.memArray;

            nSize = sendParams.memArray.Length; //memArray.Length;

            if (nSize <= sendParams.clusterSize) //TamCluster)
            {
                //TamCluster = nTam; //sí es mas chico lo que mando que el cluster
                sendParams.clusterSize = nSize;
            }

            try
            {

                while (actualPositionNumber < nSize - 1) //quizas aca me falte un byte (-1)
                {
                    conditionNumber = actualPositionNumber + sendParams.clusterSize;
                    for (int I = actualPositionNumber; I <= conditionNumber - 1; I++)
                    {
                        //meto todo al string para manadar
                        data = data + Convert.ToChar(sendParams.memArray[I]);
                        readingPositionNumber++;
                    }

                    //me re acomodo en el array
                    resultNumber = nSize - readingPositionNumber;
                    if (resultNumber <= sendParams.clusterSize)
                    {
                        sendParams.clusterSize = resultNumber; //ya estoy en el final y achico el cluster
                    }
                    /*else
                    {
                        //por ahora no hago nada
                    }*/

                    actualPositionNumber = readingPositionNumber;
                    EventParameters ev = new EventParameters();
                    ev.SetPosition(actualPositionNumber).SetEvent(EventParameters.EventType.SEND_POSITION);
                    EventSocket(ev);

                    //ACÁ ENVIO LOS DATOS
                    //Send(data, sendParams.indexConection);
                    Send(sendParams.connectionNumber, data);

                    ev.SetEvent(EventParameters.EventType.SEND_COMPLETE).SetPosition(actualPositionNumber);
                    EventSocket(ev);

                    Thread.Sleep(5);

                    data = ""; //limpio la cadena
                }//fin while

                EventParameters evSendArrayComplete = new EventParameters();
                evSendArrayComplete.SetEvent(EventParameters.EventType.SEND_ARRAY_COMPLETE);
                EventSocket(evSendArrayComplete);
            }
            catch (Exception err)
            {
                GenerateErrorEvent(err);
            }

        }

        private int GetLastSpaceFree()
        {
            int res = 0;
            if (_tcp)
            {
                res = _lstObjServerTCP.Count();
            }

            return res;
        }

        private void OrderClientList()
        {
            try
            {
                if (_tcp)
                {
                    for (int i = 0; i < _lstObjServerTCP.Count(); i++)
                    {
                        _lstObjServerTCP[i].ListIndex = i;
                    }
                }

            }
            catch (Exception err)
            {
                GenerateErrorEvent(err);
            }
        }

        /// <summary>
        /// retorna el numero de indice de la lista
        /// </summary>
        /// <param name="indiceCliente">le paso el numero de indice de cliente</param>
        /// <returns></returns>
        private int GetListIndexConnectedClientToServer(int connectionNumber)
        {
            try
            {
                if (_tcp)
                {
                    for (int i = 0; i < _lstObjServerTCP.Count(); i++)
                    {
                        if (_lstObjServerTCP[i].IndexConnection == connectionNumber)
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    //udp
                }
            }
            catch (Exception err)
            {
                GenerateErrorEvent(err);
            }
            return -1;
        }

        private void GenerateErrorEvent(Exception err, string mensajeOpcional = "")
        {
            Utils utils = new Utils();
            EventParameters ev = new EventParameters();
            if (mensajeOpcional != "")
            {
                mensajeOpcional = " " + mensajeOpcional;
            }
            ev.SetData(err.Message + mensajeOpcional).
                SetEvent(EventParameters.EventType.ERROR).
                SetErrorCode(utils.GetErrorCode(err));
            Evsocket(ev);
        }

    }
}
