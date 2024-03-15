using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSensor : MonoBehaviour
{
    public float sensorRange = 5.0f; // ������ ����
    public LayerMask vehicleLayer; // ���� ���̾�

    private int trafficCount = 0; // ���뷮 ī��Ʈ

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // ����ĳ��Ʈ �浹 Ȯ��
        if (Physics.Raycast(ray, out hit, sensorRange, vehicleLayer))
        {
            // �浹�� �����Ǹ� ������ ������ ����� ������ �����ϰ� ���뷮�� ������ŵ�ϴ�.
            trafficCount++;
        }
    }

    // ���� ���뷮�� ��ȯ�ϴ� �Լ�
    public int GetTrafficCount() 
    {
        return trafficCount;
    }
}
