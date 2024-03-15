// Traffic Simulation
// https://github.com/mchrbn/unity-traffic-simulation

using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation {
    public class Segment : MonoBehaviour {
        public List<Segment> nextSegments;
        
        [HideInInspector] public int id;
        [HideInInspector] public List<Waypoint> waypoints;

        private int vehicleCount = 0;

        public bool IsOnSegment(Vector3 _p){
            TrafficSystem ts = GetComponentInParent<TrafficSystem>();

            for(int i = 0; i < waypoints.Count - 1; i++){
                float d1 = Vector3.Distance(waypoints[i].transform.position, _p);
                float d2 = Vector3.Distance(waypoints[i + 1].transform.position, _p);
                float d3 = Vector3.Distance(waypoints[i].transform.position, waypoints[i + 1].transform.position);
                float a = (d1 + d2) - d3;
                if (a < ts.segDetectThresh && a > -ts.segDetectThresh) {
                    // 차량이 세그먼트 위에 있다고 판단되면 차량 대수를 증가시킴
                    IncrementVehicleCount();
                    return true;
                }
            }

            // 세그먼트를 벗어났을 때 차량 대수를 감소시킴
            DecrementVehicleCount();
            return false;
        }

        private void IncrementVehicleCount() {
            vehicleCount++;
        }

        private void DecrementVehicleCount() {
            vehicleCount = Mathf.Max(0, vehicleCount - 1);
        }

        public int GetVehicleCount() {
            return vehicleCount;
        }
    }
}