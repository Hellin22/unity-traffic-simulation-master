using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSensor : MonoBehaviour
{
    public float sensorRange = 5.0f; // 센서의 범위
    public LayerMask vehicleLayer; // 차량 레이어

    private int trafficCount = 0; // 교통량 카운트

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // 레이캐스트 충돌 확인
        if (Physics.Raycast(ray, out hit, sensorRange, vehicleLayer))
        {
            // 충돌이 감지되면 차량이 센서를 통과한 것으로 간주하고 교통량을 증가시킵니다.
            trafficCount++;
        }
    }

    // 현재 교통량을 반환하는 함수
    public int GetTrafficCount() 
    {
        return trafficCount;
    }
}
