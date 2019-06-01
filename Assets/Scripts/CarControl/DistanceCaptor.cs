using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// cast rays to detect how far we are from objects
public class DistanceCaptor : MonoBehaviour{

	[SerializeField] float maxDistance = 10000;

	float currentDistance = 0;

	public float CurrentDistance { get { return currentDistance / maxDistance; } }//0..1

    // Start is called before the first frame update
    void Start(){
        
    }

    // Update is called once per frame
    void FixedUpdate(){
		Ray ray = new Ray(transform.position, transform.forward);
		RaycastHit hitInfo;
		if(Physics.Raycast(ray, out hitInfo, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
			currentDistance = hitInfo.distance;
		else currentDistance = maxDistance;
    }

#if UNITY_EDITOR
	void OnDrawGizmos() {
		Gizmos.color = new Color(1, 0, 0);
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * currentDistance);
	}
#endif
}
