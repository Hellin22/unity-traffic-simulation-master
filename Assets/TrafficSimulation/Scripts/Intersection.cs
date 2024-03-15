// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.UI; // Text�� ����ϱ� ���� ���ӽ����̽� �߰�

namespace TrafficSimulation
{
    public enum IntersectionType
    {
        STOP,
        TRAFFIC_LIGHT
    }

    public class Intersection : MonoBehaviour
    {
        public IntersectionType intersectionType;
        public int id;

        //For stop only
        public List<Segment> prioritySegments;

        //For traffic lights only
        public float lightsDuration = 32;
        public float orangeLightDuration = 2;
        public List<Segment> lightsNbr1;
        public List<Segment> lightsNbr2;
        public List<Segment> lightsNbr3;
        public List<Segment> lightsNbr4;
        public GameObject[] allVehicles;
        public int[] currentGreenLightsGroups; // 1, 2, 3, 4
        public int lightsCnt;
        public int group1VehicleCount;
        public int group2VehicleCount;
        public int group3VehicleCount;
        public int group4VehicleCount;
        private int vehicleCount = 0;
        public Text vehicleCountText; // Unity Inspector���� UI Text ��Ҹ� ������ ����


        //public int light1 = 0;
        //public int light2 = 0;

        private List<GameObject> vehiclesQueueGroup1;
        private List<GameObject> vehiclesQueueGroup2;
        private List<GameObject> vehiclesQueueGroup3;
        private List<GameObject> vehiclesQueueGroup4;

        private int[] vehicleCounts; // Ŭ���� ������ �̵�
        private List<GameObject> vehiclesQueue;
        private List<GameObject> vehiclesInIntersection;
        private TrafficSystem trafficSystem;

        [HideInInspector] public int currentGreenLightsGroup = 1;

        void Start()
        {
            allVehicles = GameObject.FindObjectsOfType<GameObject>();
            vehiclesQueue = new List<GameObject>();
            vehiclesInIntersection = new List<GameObject>();
            if (lightsCnt == 3) lightsDuration = 27;
            else lightsDuration = 32;
            currentGreenLightsGroups = new int[lightsCnt];
            vehiclesQueueGroup1 = new List<GameObject>();
            vehiclesQueueGroup2 = new List<GameObject>();
            vehiclesQueueGroup3 = new List<GameObject>();
            vehiclesQueueGroup4 = new List<GameObject>();

            for (int i = 0; i < lightsCnt; i++)
            {
                currentGreenLightsGroups[i] = -1;
            }
            if (intersectionType == IntersectionType.TRAFFIC_LIGHT)
            {
                InvokeRepeating("SwitchLights", orangeLightDuration, lightsDuration + orangeLightDuration*lightsCnt);
            }
        }

        void SwitchLights()
        {
            // �� ���׸�Ʈ �׷��� ���� ��� ��������
            group1VehicleCount = GetGroupVehicleCount(lightsNbr1); 
            group2VehicleCount = GetGroupVehicleCount(lightsNbr2); 
            group3VehicleCount = GetGroupVehicleCount(lightsNbr3);
            group4VehicleCount = GetGroupVehicleCount(lightsNbr4);
            if(id == 1)
            {
                Debug.Log($"Group 1 Vehicle Count: {group1VehicleCount}" + $"Group 2 Vehicle Count: {group2VehicleCount}" + $"Group 3 Vehicle Count: {group3VehicleCount}" + $"Group 4 Vehicle Count: {group4VehicleCount}");
            }

            vehicleCounts = new int[] { group1VehicleCount, group2VehicleCount, group3VehicleCount, group4VehicleCount };

            // �����ϱ� ���� �� �׷��� �ε����� ����
            int[] groupIndexes = Enumerable.Range(0, vehicleCounts.Length).ToArray();

            // ���� ���� �������� �׷� �ε����� ����
            Array.Sort(groupIndexes, (a, b) => vehicleCounts[b].CompareTo(vehicleCounts[a]));

            // ���ĵ� �׷� �ε����� currentGreenLightsGroups �迭�� ����
            for (int i = 0; i < lightsCnt; i++)
            {
                currentGreenLightsGroups[i] = groupIndexes[i] + 1;
            }

            StartCoroutine(SwitchLightsCoroutine());
        }

        IEnumerator SwitchLightsCoroutine()
        {
            for (int i = 0; i < lightsCnt; i++)
            {
                currentGreenLightsGroup = currentGreenLightsGroups[i];

                int currentGroupVehicleCount = vehicleCounts[currentGreenLightsGroup - 1];
                float waitTime = CalculateWaitTime(currentGroupVehicleCount);
                if (id == 1)
                {
                    Debug.Log($"Group {currentGreenLightsGroup}�� ��� �ð���: {waitTime}, ���� ���: {currentGroupVehicleCount}");
                }

                // Get the corresponding vehicles queue for the current group
                List<GameObject> currentGroupQueue = GetVehiclesQueueForGroup(currentGreenLightsGroup);

                // ��� �ð���ŭ ����ϸ鼭 MoveVehiclesQueue ȣ��
                yield return StartCoroutine(WaitAndMoveRoutine(orangeLightDuration, waitTime, currentGroupQueue));
            }
        }
        float CalculateWaitTime(int groupVehicleCount)
        {
            // ������ ���� ��� ��� �ð��� 0���� ����
            if (groupVehicleCount == 0)
            {
                return lightsDuration / lightsCnt;
                //return 0f;
            }
            float totalVehicleCount = group1VehicleCount + group2VehicleCount + group3VehicleCount + group4VehicleCount; // ���⼭ �߸��ȵ�. lightsNbr1�� count�� lightsNbr1�� �����ݾ�. �׷��ϱ� ���׸�Ʈ ����Ʈ 1���ۿ� �����ϱ� ������. �̰� �ƴ�.
            float groupRatio = (float)groupVehicleCount / totalVehicleCount;
            float waitTime = lightsDuration * groupRatio;
            waitTime = lightsDuration / lightsCnt;
            if (id == 1)
            {
                Debug.Log($"��ü ��������� : {totalVehicleCount}, Group1�� ��������� : {group1VehicleCount}, Group2�� ��������� : {group2VehicleCount}, Group3�� ��������� : {group3VehicleCount}, Group4�� ��������� : {group4VehicleCount}, �ش� �׷��� ��������� : {groupVehicleCount}, ������ : {groupRatio}, �ʷϺҿ� �ɸ��� ���ð��� : {waitTime}");
            }
            // �� �׷쿡 �Ҵ�Ǵ� ��� �ð� ���

            return waitTime;
        }

        IEnumerator WaitAndMoveRoutine(float orangeLightDuration, float waitTime, List<GameObject> queue)
        {
            // MoveVehiclesQueue ȣ��
            MoveVehiclesQueue(queue);

            // ��� �ð���ŭ ���
            yield return new WaitForSeconds(waitTime);

            // ���� �׷����� �Ѿ�� ���� ���
            yield return new WaitForSeconds(orangeLightDuration);
        }


        void MoveVehiclesQueue(List<GameObject> queue)
        {
            if(id == 1)
            {
                // LogAllQueues(); // ���� ť�� ���� ���
            }
            
            queue.RemoveAll(vehicle =>
            {
                int vehicleSegment = vehicle.GetComponent<VehicleAI>().GetSegmentVehicleIsIn();
                if (IsGreenLightSegment(vehicleSegment))
                {
                    vehicle.GetComponent<VehicleAI>().vehicleStatus = Status.GO;
                    // currentGreenLightsGroup�� ���� ť ��ȣ ����
                    List<GameObject> currentGroupQueue = GetVehiclesQueueForGroup(currentGreenLightsGroup);
                    currentGroupQueue.Remove(vehicle);

                    return true; // ť���� ���� ����
                }

                return false; // ť�� ���� ����
            });
        }

        

        // �ش� �׷쿡 ���� ���׸�Ʈ ����Ʈ ��������
        List<GameObject> GetVehiclesQueueForGroup(int group)
        {
            switch (group)
            {
                case 1:
                    return vehiclesQueueGroup1;
                case 2:
                    return vehiclesQueueGroup2;
                case 3:
                    return vehiclesQueueGroup3;
                case 4:
                    return vehiclesQueueGroup4;
                default:
                    return new List<GameObject>();
            }
        }

        // ���׸�Ʈ �׷��� ���� ����� �������� �Լ�
        int GetGroupVehicleCount(List<Segment> group)
        {
            int count = 0;

            // �� ������ Ȯ���ϸ鼭 ���ϴ� ���׸�Ʈ�� ���ϴ��� ���θ� �Ǵ�
            foreach (GameObject vehicle in allVehicles)
            {
                VehicleAI vehicleAI = vehicle.GetComponent<VehicleAI>();

                if (vehicleAI != null && IsVehicleInGroup(vehicleAI, group))
                {
                    count++;
                }
            }

            return count;
        }

        bool IsVehicleInGroup(VehicleAI vehicle, List<Segment> group)
        {
            int segmentId = vehicle.GetSegmentVehicleIsIn();

            // ������ Ư�� �׷쿡 ���ϴ��� ���� �Ǵ�
            return group.Any(segment => segment.id == segmentId);
        }


        void UpdateVehicleCountText()
        {
            if (vehicleCountText != null)
            {
                vehicleCountText.text = vehicleCount.ToString();
            }
        }

        void OnTriggerEnter(Collider _other)
        {
            //Check if vehicle is already in the list if yes abort
            //Also abort if we just started the scene (if vehicles inside colliders at start)
            if (IsAlreadyInIntersection(_other.gameObject) || Time.timeSinceLevelLoad < .5f) return;

            if (_other.tag == "AutonomousVehicle" && intersectionType == IntersectionType.STOP)
                TriggerStop(_other.gameObject);
            else if (_other.tag == "AutonomousVehicle" && intersectionType == IntersectionType.TRAFFIC_LIGHT)
            {
                TriggerLight(_other.gameObject);
            }
        }

        void OnTriggerExit(Collider _other)
        {
            if (_other.tag == "AutonomousVehicle" && intersectionType == IntersectionType.STOP)
                ExitStop(_other.gameObject);
            else if (_other.tag == "AutonomousVehicle" && intersectionType == IntersectionType.TRAFFIC_LIGHT)
                ExitLight(_other.gameObject);
        }

        void TriggerStop(GameObject _vehicle)
        {
            VehicleAI vehicleAI = _vehicle.GetComponent<VehicleAI>();

            // Depending on the waypoint threshold, the car can be either on the target segment or on the past segment
            int vehicleSegment = vehicleAI.GetSegmentVehicleIsIn();

            if (!IsPrioritySegment(vehicleSegment))
            {
                if (GetVehiclesQueueForGroup(currentGreenLightsGroup).Count > 0 || vehiclesInIntersection.Count > 0)
                {
                    vehicleAI.vehicleStatus = Status.STOP;
                    GetVehiclesQueueForGroup(currentGreenLightsGroup).Add(_vehicle);
                }
                else
                {
                    vehiclesInIntersection.Add(_vehicle);
                    vehicleAI.vehicleStatus = Status.SLOW_DOWN;
                }
            }
            else
            {
                vehicleAI.vehicleStatus = Status.SLOW_DOWN;
                vehiclesInIntersection.Add(_vehicle);
            }
        }

        void ExitStop(GameObject _vehicle)
        {
            _vehicle.GetComponent<VehicleAI>().vehicleStatus = Status.GO;
            vehiclesInIntersection.Remove(_vehicle);

            List<GameObject> currentGroupQueue = GetVehiclesQueueForGroup(currentGreenLightsGroup);

            currentGroupQueue.Remove(_vehicle);

            if (currentGroupQueue.Count > 0 && vehiclesInIntersection.Count == 0)
            {
                currentGroupQueue[0].GetComponent<VehicleAI>().vehicleStatus = Status.GO;
            }
        }

        void TriggerLight(GameObject _vehicle)
        {
            VehicleAI vehicleAI = _vehicle.GetComponent<VehicleAI>();
            int vehicleSegment = vehicleAI.GetSegmentVehicleIsIn();

            if (IsGreenLightSegment(vehicleSegment))
            {
                vehicleAI.vehicleStatus = Status.GO;

                // ����� �α� �߰�: ������ ����
                List<GameObject> currentGroupQueue = GetVehiclesQueueForGroup(currentGreenLightsGroup); 
                if(id == 1)
                {
                    //Debug.Log("���� intersection id = " + id +"�̰� ���� ������ segment �׷� = " + vehicleSegment + "�Դϴ�. �ʷϺ��� �����̹Ƿ� ť�� �߰����� �ʽ��ϴ�. ");
                }
                currentGroupQueue.Remove(_vehicle);
            }
            else
            {
                // �������� ��� �ش� ���׸�Ʈ �׷쿡 �˸´� ť�� ���� �߰�
                vehicleAI.vehicleStatus = Status.STOP;
                List<GameObject> currentGroupQueue = vehiclesQueueGroup1; // �Ҵ縸 ���ذ�.

                // ��� intersection���� 1���� ���, 2���� ���, 3���� ���, 4���� ��쿡 ���� ����ǵ��� �ϱ�
                if (vehicleSegment == 8 || vehicleSegment == 12 || vehicleSegment == 13 || vehicleSegment == 21 || vehicleSegment == 15)
                {
                    currentGroupQueue = vehiclesQueueGroup1;
                }
                else if(vehicleSegment == 10 || vehicleSegment == 33 || vehicleSegment == 16 || vehicleSegment == 17 || vehicleSegment == 2)
                {
                    currentGroupQueue = vehiclesQueueGroup2;
                }
                else if(vehicleSegment == 9 || vehicleSegment == 24 || vehicleSegment == 25 || vehicleSegment == 23 || vehicleSegment == 28)
                {
                    currentGroupQueue = vehiclesQueueGroup3;
                }
                else if(vehicleSegment == 14)
                {
                    currentGroupQueue = vehiclesQueueGroup4;
                }
                if (id == 1)
                {
                    //Debug.Log("���� intersection id = " + id + "�̰� ���� ������ segment �׷� = " + vehicleSegment + "�Դϴ�. �������� �����̹Ƿ� ť�� �߰��մϴ�. " + $"Vehicle {vehicleAI.name} added to group {vehicleSegment} queue.");
                }
                currentGroupQueue.Add(_vehicle);
            }
        }

        void ExitLight(GameObject _vehicle)
        {
            // ���� �̵� Ƚ�� ����
            vehicleCount++;
            // UI Text ������Ʈ
            UpdateVehicleCountText();
            _vehicle.GetComponent<VehicleAI>().vehicleStatus = Status.GO;
        }


        bool IsGreenLightSegment(int _vehicleSegment)
        {
            if (currentGreenLightsGroup == 1)
            {
                foreach (Segment segment in lightsNbr1)
                {
                    if (segment.id == _vehicleSegment)
                        return true;
                }
            }
            else if (currentGreenLightsGroup == 2)
            {
                foreach (Segment segment in lightsNbr2)
                {
                    if (segment.id == _vehicleSegment)
                        return true;
                }
            }
            else if (currentGreenLightsGroup == 3)
            {
                foreach (Segment segment in lightsNbr3)
                {
                    if (segment.id == _vehicleSegment)
                        return true;
                }
            }
            else // currentGreenLightsGroup == 4
            {
                foreach (Segment segment in lightsNbr4)
                {
                    if (segment.id == _vehicleSegment)
                        return true;
                }
            }
            return false;
        }




        bool IsPrioritySegment(int _vehicleSegment)
        {
            foreach (Segment s in prioritySegments)
            {
                if (_vehicleSegment == s.id)
                    return true;
            }
            return false;
        }

        bool IsAlreadyInIntersection(GameObject _target)
        {
            foreach (GameObject vehicle in vehiclesInIntersection)
            {
                if (vehicle.GetInstanceID() == _target.GetInstanceID()) return true;
            }

            // �� �׷캰 ť���� ���� ���� ���� Ȯ��
            for (int i = 1; i <= lightsCnt; i++)
            {
                List<GameObject> currentGroupQueue = GetVehiclesQueueForGroup(i);
                foreach (GameObject vehicle in currentGroupQueue)
                {
                    if (vehicle.GetInstanceID() == _target.GetInstanceID()) return true;
                }
            }

            return false;
        }


        private List<GameObject> memVehiclesQueueGroup1 = new List<GameObject>();
        private List<GameObject> memVehiclesQueueGroup2 = new List<GameObject>();
        private List<GameObject> memVehiclesQueueGroup3 = new List<GameObject>();
        private List<GameObject> memVehiclesQueueGroup4 = new List<GameObject>();
        private List<GameObject> memVehiclesInIntersection = new List<GameObject>();

        public void SaveIntersectionStatus()
        {
            memVehiclesQueueGroup1 = new List<GameObject>(vehiclesQueueGroup1);
            memVehiclesQueueGroup2 = new List<GameObject>(vehiclesQueueGroup2);
            memVehiclesQueueGroup3 = new List<GameObject>(vehiclesQueueGroup3);
            memVehiclesQueueGroup4 = new List<GameObject>(vehiclesQueueGroup4);
            memVehiclesInIntersection = new List<GameObject>(vehiclesInIntersection);
        }

        public void ResumeIntersectionStatus()
        {
            foreach (GameObject v in vehiclesInIntersection)
            {
                foreach (GameObject v2 in memVehiclesInIntersection)
                {
                    if (v.GetInstanceID() == v2.GetInstanceID())
                    {
                        v.GetComponent<VehicleAI>().vehicleStatus = v2.GetComponent<VehicleAI>().vehicleStatus;
                        break;
                    }
                }
            }

            foreach (GameObject v in vehiclesQueueGroup1)
            {
                foreach (GameObject v2 in memVehiclesQueueGroup1)
                {
                    if (v.GetInstanceID() == v2.GetInstanceID())
                    {
                        v.GetComponent<VehicleAI>().vehicleStatus = v2.GetComponent<VehicleAI>().vehicleStatus;
                        break;
                    }
                }
            }
            foreach (GameObject v in vehiclesQueueGroup2)
            {
                foreach (GameObject v2 in memVehiclesQueueGroup2)
                {
                    if (v.GetInstanceID() == v2.GetInstanceID())
                    {
                        v.GetComponent<VehicleAI>().vehicleStatus = v2.GetComponent<VehicleAI>().vehicleStatus;
                        break;
                    }
                }
            }

            foreach (GameObject v in vehiclesQueueGroup3)
            {
                foreach (GameObject v2 in memVehiclesQueueGroup3)
                {
                    if (v.GetInstanceID() == v2.GetInstanceID())
                    {
                        v.GetComponent<VehicleAI>().vehicleStatus = v2.GetComponent<VehicleAI>().vehicleStatus;
                        break;
                    }
                }
            }

            foreach (GameObject v in vehiclesQueueGroup4)
            {
                foreach (GameObject v2 in memVehiclesQueueGroup4)
                {
                    if (v.GetInstanceID() == v2.GetInstanceID())
                    {
                        v.GetComponent<VehicleAI>().vehicleStatus = v2.GetComponent<VehicleAI>().vehicleStatus;
                        break;
                    }
                }
            }
        }
        public void LogAllQueues()
        {
            if(id == 1)
            {
                Debug.Log("Queue Group 1: " + string.Join(", ", vehiclesQueueGroup1.Select(vehicle => vehicle.name)));
                Debug.Log("Queue Group 2: " + string.Join(", ", vehiclesQueueGroup2.Select(vehicle => vehicle.name)));
                Debug.Log("Queue Group 3: " + string.Join(", ", vehiclesQueueGroup3.Select(vehicle => vehicle.name)));
                Debug.Log("Queue Group 4: " + string.Join(", ", vehiclesQueueGroup4.Select(vehicle => vehicle.name)));
                Debug.Log("Vehicles In Intersection: " + string.Join(", ", vehiclesInIntersection.Select(vehicle => vehicle.name)));
            }
        }
    }
}