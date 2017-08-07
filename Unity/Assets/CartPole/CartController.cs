using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
x : 트랙상의 카트 위치
θ : 수직과 극의 각도
dx / dt : 카트 속도
dθ / dt : 각도 변화율
*/

public class State
{
    public float position = 0;      // x : 트랙상의 카트 위치
    public float speed = 0;         // dx / dt : 카트 속도

    public float angle = 0;         // θ : 수직과 극의 각도
    public float angle_speed = 0;   // dθ / dt : 각도 변화율
    float last_angle = 0;
    public float reward = 0;

    public int isDone = 0;
    public int episode = 0;
    public void UpdateAngleSpeed()
    {
        angle_speed = (angle - last_angle) / Time.deltaTime;
        last_angle = angle;
    }

    public void reset()
    {
        episode = 0;
        position = 0;
        speed = 0;
        angle = 0;
        last_angle = 0;
        angle_speed = 0;
        reward = 0;
    }
}

public class CartController : MonoBehaviour {

    public float failAngel = 70;
    public int max_Step = 3000;
    public Rigidbody pole;
    public Rigidbody weight;
    private Rigidbody cart;

    private SendPacket sendPacket;
    private State state;
    private CharacterController controller;

    private Vector3 cart_originPos;
    private Vector3 pole_originPos;
    private Vector3 weight_originPos;
    private float r;


    void Start ()
    {
        controller = GetComponent<CharacterController>();
        cart = GetComponent<Rigidbody>();
        sendPacket = new SendPacket();

        state = new State();
        r = 2f;

        cart_originPos = transform.position;
        pole_originPos = pole.transform.position;
        weight_originPos = weight.transform.position;
        StartCoroutine(StartCartpole());
    }

    IEnumerator StartCartpole()
    {
        while(true)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
                break;
            yield return new WaitForFixedUpdate();
        }
        ResetEpisode();
    }

    IEnumerator StartResetEpisode()
    {
        yield return new WaitForFixedUpdate();
        ResetEpisode();
    }

    public IEnumerator Action(double actionValue)
    {
        if (actionValue > 0.5d)
            cart.AddForce(Vector3.right * 100);
        else
            cart.AddForce(Vector3.left * 100);
        state.episode++;
        yield return new WaitForFixedUpdate();

        UpdateState();
        CheckFail();
    }

    bool IsMaxStep()
    {
        if (state.episode > max_Step)
            return true;
        return false;
    }

    public void CheckFail()
    {
        if(pole.transform.eulerAngles.z > failAngel && pole.transform.eulerAngles.z < 360 - failAngel || IsMaxStep())
        {
            state.isDone = 1;
            SocketServer.instance.SendMessage(ConvertData());
            //한 에피소드씩 재생할 때 사용
            //StartCoroutine(StartCartpole());
            StartCoroutine(StartResetEpisode());
            //ResetEpisode();
        }
        else
            SocketServer.instance.SendMessage(ConvertData());
    }

    void UpdateState()
    {
        state.position = cart.position.x;
        state.speed = cart.velocity.x;

        float height = weight.position.y - cart.position.y;
        float x = weight.position.x - cart.position.x;
        state.angle = Mathf.Atan2(x, height) * Mathf.Rad2Deg;
        state.UpdateAngleSpeed();
    }

    void ResetEpisode()
    {
        state.reset();
        pole.transform.position = pole_originPos;
        weight.transform.position = weight_originPos;
        cart.transform.position = cart_originPos;

        pole.transform.rotation = Quaternion.identity;
        weight.transform.rotation = Quaternion.identity;
        cart.transform.rotation = Quaternion.identity;

        pole.velocity = Vector3.zero;
        weight.velocity = Vector3.zero;
        cart.velocity = Vector3.zero;

        pole.angularVelocity = Vector3.zero;
        weight.angularVelocity = Vector3.zero;
        cart.angularVelocity = Vector3.zero;
        state.isDone = 0;
        SocketServer.instance.SendMessage(ConvertData());
    }

    public SendPacket ConvertData()
    {
        /*
        sendPacket.data1 = Mathf.Round(state.position * 100000d) * 0.00001d;
        sendPacket.data2 = Mathf.Round(Mathf.Deg2Rad * state.angle * 100000) *0.000001d;
        sendPacket.data3 = Mathf.Round(state.speed * 100000) *0.00001d;
        sendPacket.data4 = Mathf.Round(Mathf.Deg2Rad * state.angle_speed * 100000) *0.000001d;
        sendPacket.data5 = state.isDone;
        */
        sendPacket.data1 = state.position;
        sendPacket.data2 = Mathf.Deg2Rad * state.angle;
        sendPacket.data3 = state.speed * 0.1f;
        sendPacket.data4 = Mathf.Deg2Rad * state.angle_speed * 0.1f;
        sendPacket.data5 = state.isDone;


        return sendPacket;
    }

    
}
