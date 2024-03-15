// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.UI; // Text를 사용하기 위한 네임스페이스 추가

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
        public Text vehicleCountText; // Unity Inspector에서 UI Text 요소를 연결할 변수


        //public int light1 = 0;
        //public int light2 = 0;

        private List<GameObject> vehiclesQueueGroup1;
        private List<GameObject> vehiclesQueueGroup2;
        private List<GameObject> vehiclesQueueGroup3;
        private List<GameObject> vehiclesQueueGroup4;

        private int[] vehicleCounts; // 클래스 변수로 이동
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
            // 각 세그먼트 그룹의 차량 대수 가져오기
            group1VehicleCount = GetGroupVehicleCount(lightsNbr1); 
            group2VehicleCount = GetGroupVehicleCount(lightsNbr2); 
            group3VehicleCount = GetGroupVehicleCount(lightsNbr3);
            group4VehicleCount = GetGroupVehicleCount(lightsNbr4);
            if(id == 1)
            {
                Debug.Log($"Group 1 Vehicle Count: {group1VehicleCount}" + $"Group 2 Vehicle Count: {group2VehicleCount}" + $"Group 3 Vehicle Count: {group3VehicleCount}" + $"Group 4 Vehicle Count: {group4VehicleCount}");
            }

            vehicleCounts = new int[] { group1VehicleCount, group2VehicleCount, group3VehicleCount, group4VehicleCount };

            // 정렬하기 위해 각 그룹의 인덱스를 생성
            int[] groupIndexes = Enumerable.Range(0, vehicleCounts.Length).ToArray();

            // 차량 수를 기준으로 그룹 인덱스를 정렬
            Array.Sort(groupIndexes, (a, b) => vehicleCounts[b].CompareTo(vehicleCounts[a]));

            // 정렬된 그룹 인덱스를 currentGreenLightsGroups 배열에 저장
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
                    Debug.Log($"Group {currentGreenLightsGroup}의 대기 시간은: {waitTime}, 차량 대수: {currentGroupVehicleCount}");
                }

                // Get the corresponding vehicles queue for the current group
                List<GameObject> currentGroupQueue = GetVehiclesQueueForGroup(currentGreenLightsGroup);

                // 대기 시간만큼 대기하면서 MoveVehiclesQueue 호출
                yield return StartCoroutine(WaitAndMoveRoutine(orangeLightDuration, waitTime, currentGroupQueue));
            }
        }
        float CalculateWaitTime(int groupVehicleCount)
        {
            // 차량이 없는 경우 대기 시간을 0으로 리턴
            if (groupVehicleCount == 0)
            {
                return lightsDuration / lightsCnt;
                //return 0f;
            }
            float totalVehicleCount = group1VehicleCount + group2VehicleCount + group3VehicleCount + group4VehicleCount; // 여기서 잘못된듯. lightsNbr1의 count는 lightsNbr1의 개수잖아. 그러니까 세그먼트 리스트 1개밖에 없으니까 저래됨. 이거 아님.
            float groupRatio = (float)groupVehicleCount / totalVehicleCount;
            float waitTime = lightsDuration * groupRatio;
            waitTime = lightsDuration / lightsCnt;
            if (id == 1)
            {
                Debug.Log($"전체 차량대수는 : {totalVehicleCount}, Group1의 차량대수는 : {group1VehicleCount}, Group2의 차량대수는 : {group2VehicleCount}, Group3의 차량대수는 : {group3VehicleCount}, Group4의 차량대수는 : {group4VehicleCount}, 해당 그룹의 차량대수는 : {groupVehicleCount}, 비율은 : {groupRatio}, 초록불에 걸리는 대기시간은 : {waitTime}");
            }
            // 각 그룹에 할당되는 대기 시간 계산

            return waitTime;
        }

        IEnumerator WaitAndMoveRoutine(float orangeLightDuration, float waitTime, List<GameObject> queue)
        {
            // MoveVehiclesQueue 호출
            MoveVehiclesQueue(queue);

            // 대기 시간만큼 대기
            yield return new WaitForSeconds(waitTime);

            // 다음 그룹으로 넘어가기 전에 대기
            yield return new WaitForSeconds(orangeLightDuration);
        }


        void MoveVehiclesQueue(List<GameObject> queue)
        {
            if(id == 1)
            {
                // LogAllQueues(); // 현재 큐의 상태 출력
            }
            
            queue.RemoveAll(vehicle =>
            {
                int vehicleSegment = vehicle.GetComponent<VehicleAI>().GetSegmentVehicleIsIn();
                if (IsGreenLightSegment(vehicleSegment))
                {
                    vehicle.GetComponent<VehicleAI>().vehicleStatus = Status.GO;
                    // currentGreenLightsGroup에 따라서 큐 번호 결정
                    List<GameObject> currentGroupQueue = GetVehiclesQueueForGroup(currentGreenLightsGroup);
                    currentGroupQueue.Remove(vehicle);

                    return true; // 큐에서 차량 제거
                }

                return false; // 큐에 차량 유지
            });
        }

        

        // 해당 그룹에 대한 세그먼트 리스트 가져오기
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

        // 세그먼트 그룹의 차량 대수를 가져오는 함수
        int GetGroupVehicleCount(List<Segment> group)
        {
            int count = 0;

            // 각 차량을 확인하면서 원하는 세그먼트에 속하는지 여부를 판단
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

            // 차량이 특정 그룹에 속하는지 여부 판단
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

                // 디버그 로그 추가: 차량을 없앰
                List<GameObject> currentGroupQueue = GetVehiclesQueueForGroup(currentGreenLightsGroup); 
                if(id == 1)
                {
                    //Debug.Log("현재 intersection id = " + id +"이고 현재 차량의 segment 그룹 = " + vehicleSegment + "입니다. 초록불인 상태이므로 큐에 추가하지 않습니다. ");
                }
                currentGroupQueue.Remove(_vehicle);
            }
            else
            {
                // 빨간불인 경우 해당 세그먼트 그룹에 알맞는 큐에 차량 추가
                vehicleAI.vehicleStatus = Status.STOP;
                List<GameObject> currentGroupQueue = vehiclesQueueGroup1; // 할당만 해준것.

                // 모든 intersection에서 1번인 경우, 2번인 경우, 3번인 경우, 4번인 경우에 따라서 적용되도록 하기
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
                    //Debug.Log("현재 intersection id = " + id + "이고 현재 차량의 segment 그룹 = " + vehicleSegment + "입니다. 빨간불인 상태이므로 큐에 추가합니다. " + $"Vehicle {vehicleAI.name} added to group {vehicleSegment} queue.");
                }
                currentGroupQueue.Add(_vehicle);
            }
        }

        void ExitLight(GameObject _vehicle)
        {
            // 차량 이동 횟수 증가
            vehicleCount++;
            // UI Text 업데이트
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

            // 각 그룹별 큐에서 차량 존재 여부 확인
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