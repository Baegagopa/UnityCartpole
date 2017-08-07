using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections;

[Serializable]
public struct SendPacket
{
    public float data1;    // x : 트랙상의 카트 위치
    public float data2;    // θ : 수직과 극의 각도
    public float data3;    // dx / dt : 카트 속도
    public float data4;    // dθ / dt : 각도 변화율
    public float data5;    // 끝났는가
}

[Serializable]
public struct RecvPacket
{
    public float data1;    // 입력
}

class SocketServer : MonoBehaviour
{
    public static SocketServer instance;
    public CartController cart;
    Socket SeverSocket = null;
    Thread Socket_Thread = null;
    bool Socket_Thread_Flag = false;

    SendPacket sendData;
    RecvPacket recvData;
    bool isRecved;

    IPEndPoint ipep;
    Socket client;
    IPEndPoint clientep;
    NetworkStream recvStm;
    NetworkStream sendStm;

    void Awake()
    {
        instance = this;
        Socket_Thread = new Thread(Dowrk);
        Socket_Thread_Flag = true;
        Socket_Thread.Start();

        isRecved = false;
        recvData = new RecvPacket();
        sendData = new SendPacket();


        sendData.data1 = 0;
        sendData.data2 = 0;
        sendData.data3 = 0;
        sendData.data4 = 0;
        sendData.data5 = 0;

    }

    private void FixedUpdate()
    {
        if (isRecved)
        {
            StartCoroutine(cart.Action(recvData.data1));
            isRecved = false;
        }
    }


    // byte 배열을 구조체로
    public static T ByteToStruct<T>(byte[] buffer) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));

        if (size > buffer.Length)
        {
            throw new Exception();
        }

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(buffer, 0, ptr, size);
        T obj = (T)Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);
        return obj;
    }

    // 구조체를 byte 배열로
    public static byte[] StructureToByte(object obj)
    {
        int datasize = Marshal.SizeOf(obj);           //((PACKET_DATA)obj).TotalBytes; // 구조체에 할당된 메모리의 크기를 구한다.
        IntPtr buff = Marshal.AllocHGlobal(datasize); // 비관리 메모리 영역에 구조체 크기만큼의 메모리를 할당한다.
        Marshal.StructureToPtr(obj, buff, false);     // 할당된 구조체 객체의 주소를 구한다.
        byte[] data = new byte[datasize];             // 구조체가 복사될 배열
        Marshal.Copy(buff, data, 0, datasize);        // 구조체 객체를 배열에 복사
        Marshal.FreeHGlobal(buff);                    // 비관리 메모리 영역에 할당했던 메모리를 해제함
        return data;                                  // 배열을 리턴
    }


    private void Dowrk()
    {
        SeverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        ipep = new IPEndPoint(IPAddress.Any, 8080);
        SeverSocket.Bind(ipep);
        SeverSocket.Listen(10);

        Debug.Log("소켓 대기중....");
        client = SeverSocket.Accept();//client에서 수신을 요청하면 접속합니다.
        Debug.Log("소켓 연결되었습니다.");

        clientep = (IPEndPoint)client.RemoteEndPoint;
        recvStm = new NetworkStream(client);
        sendStm = new NetworkStream(client);

        while (Socket_Thread_Flag)
        {
            byte[] receiveBuffer = new byte[256];
            try
            {
                recvStm.Read(receiveBuffer, 0, receiveBuffer.Length);
                recvData = ByteToStruct<RecvPacket>(receiveBuffer);
                isRecved = true;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Socket_Thread_Flag = false;
                client.Close();
                SeverSocket.Close();
                continue;
            }
        }
    }

    public void SendMessage(SendPacket sendData)
    {
        Debug.Log(Marshal.SizeOf(sendData) +" : "+sendData.data1 + " " + sendData.data2 + " " + sendData.data3 + " " + sendData.data4 + " " + sendData.data5);
        byte[] packetArray = StructureToByte(sendData);
        sendStm.Write(packetArray, 0, packetArray.Length);
    }

    void OnApplicationQuit()
    {
        try
        {
            Socket_Thread_Flag = false;
            Socket_Thread.Abort();
            SeverSocket.Close();
        }
        catch
        {
            Debug.Log("소켓과 쓰레드 종료때 오류가 발생");
        }
    }


}