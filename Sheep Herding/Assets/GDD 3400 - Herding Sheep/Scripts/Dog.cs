using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace GDD3400.Project01
{
    public class Dog : MonoBehaviour
    {

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        // Required Variables (Do not edit!)
        private float _maxSpeed = 5f;
        private float _sightRadius = 10f;

        // Layers - Set In Project Settings
        public LayerMask _targetsLayer;
        public LayerMask _obstaclesLayer;

        // Tags - Set In Project Settings
        private const string friendTag = "Friend";
        private const string threatTag = "Threat";
        private const string safeZoneTag = "SafeZone";

        //basic variables
        private Rigidbody rb;
        public GameObject safeZone;
        private Vector3 safeZonePos;
        


        //patrol variables 
        private bool didPatrol = false;
        private Vector3 centerPoint = new Vector3(0f, 0, 0); //wooo center
        private Vector3 wayPoints0 = new Vector3(-13, 0, 13);
        private Vector3 wayPoints1 = new Vector3(-13, 0, -13);
        private Vector3 wayPoints2 = new Vector3(13, 0, -13);
        private Vector3 wayPoints3 = new Vector3(13, 0, 13);
        public int currentIndex = 0;


        private float angle = 0f; // Current angle in radians
        private float currentRotation = 0f;
        [SerializeField] public float radius = 5f;
        [SerializeField] public float rotSpeed = 1f;//how fast to circle

        //face direction
        public float turnSpeed = 5f;
        public Vector3 perpendicularOffset = new Vector3(0, 90, 0); //know dir per to center so we can face that dir

        //seek and chase variables
        public float distanceBehind = 5f;
        private float arrivalThreshold = 0.1f;
        private Transform closestSheep;
        public Vector3 tempTarget;

        private bool foundASheep = false;
        private bool headTowardsCenter = true;
        private bool startChase = false;
        private Vector3 currentDirection;


        public void Awake()
        {
            // Find the layers in the project settings
            _targetsLayer = LayerMask.GetMask("Targets");
            _obstaclesLayer = LayerMask.GetMask("Obstacles");

        }
        public void Start()
        {
            rb = GetComponent<Rigidbody>();

            //make sure that we start the initial patrol from whatever point we spawned at
            Vector3 initialOffset = transform.position - centerPoint;
            angle = Mathf.Atan2(initialOffset.z, initialOffset.x);

            GameObject SafeZone = GameObject.FindGameObjectWithTag("SafeZone");
            safeZonePos = SafeZone.transform.position;

            //initialize waypoint array

        }

        private void Update()
        {
            if (!_isActive) return;


            DecisionMaking();

            Vector3 currentPos = transform.position;
            if (currentPos == centerPoint)
            {
                currentIndex = 1;
            }

            if (currentPos == wayPoints0) 
            {
                currentIndex += 1;
            }
            if (currentPos == wayPoints1)
            {
                currentIndex += 1;
            }
            if (currentPos == wayPoints2)
            {
                currentIndex += 1;
            }
            if (currentPos == wayPoints3)
            {
                currentIndex += 1;
            }

        }

        private void Perception()
        {

        }

        private void DecisionMaking()
        {
            //do a wide circle around the map so that the sheep naturally bunch together from the dog coming in sight (ideally)
            if (didPatrol == false)
            {
                initialPatrol();
            }
            if (didPatrol == true && headTowardsCenter == true)
            {
                goToPoint(centerPoint);
                Vector3 currentPos = transform.position;
                if (currentPos == centerPoint) { headTowardsCenter = false; startChase = true; }
            }

        }
        private void initialPatrol()
        {
            // Update the angle based on speed and time
            angle += rotSpeed * Time.deltaTime;

            // Calculate the new position on the circle
            float x = centerPoint.x + radius * Mathf.Cos(angle);
            float z = centerPoint.z + radius * Mathf.Sin(angle); // Use Z for horizontal movement

            // Maintain the original Y-position (or adjust if needed)
            float y = transform.position.y;

            // Keep track of the total rotation
            currentRotation += rotSpeed * Time.deltaTime;

            // Set the object's new position
            transform.position = new Vector3(x, y, z);

            // Optional: Make the object always face the center point

            if (currentRotation >= 6.3f)
            {
                didPatrol = true;
            }

            // Make the object look directly at the center point
            transform.LookAt(centerPoint);

            // Apply an additional rotation to make it perpendicular
            transform.Rotate(perpendicularOffset, Space.Self);
        }
        private void goToPoint(Vector3 point)
        {

            //this.gameObject.tag = friendTag;
            //go towards the center of the playspace
            //ideally makes positioning for finding and rounding sheep towards safezone easier
            Vector3 directionToTarget = point - transform.position;
            directionToTarget.y = 0; // 

            //calculate the target rotation
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            //rotate towards the target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            //move towards center
            transform.position = Vector3.MoveTowards(transform.position, point, _maxSpeed * Time.deltaTime);
        }
        void FindClosestSheep()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, _sightRadius);
            float maxDistance = 0f;
            closestSheep = null;

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Friend"))
                {
                    float currentDistance = Vector3.Distance(transform.position, hitCollider.transform.position);
                    if (currentDistance > maxDistance)
                    {
                        maxDistance = currentDistance;
                        closestSheep = hitCollider.transform;
                    }
                }
            }
            Debug.Log(closestSheep.name);
            foundASheep = true;
        }

        /// <summary>
        /// Make sure to use FixedUpdate for movement with physics based Rigidbody
        /// You can optionally use FixedDeltaTime for movement calculations, but it is not required since fixedupdate is called at a fixed rate
        /// </summary>
        private void FixedUpdate()
        {
            //Vector3 currentPos = transform.position;
            //if (currentPos == centerPoint) { currentIndex = 0; }

            if (!_isActive) return;
            if (startChase == true)
            {
                if (foundASheep == false)
                {
                    FindClosestSheep();
                }
                if (foundASheep)
                {
                    transform.LookAt(closestSheep);

                    if (closestSheep != null)//holy fuck
                    {
                        // Get the positions of the two points
                        Vector3 pointA = safeZonePos;
                        Vector3 pointB = closestSheep.position;

                        // 1. Calculate the direction vector from A to B
                        Vector3 directionVector = pointB - pointA;

                        // 2. Normalize the direction vector
                        Vector3 normalizedDirection = directionVector.normalized;

                        // 3. Calculate the extended point
                        Vector3 extendedPoint = pointB + (normalizedDirection * distanceBehind);
                        transform.position = Vector3.MoveTowards(transform.position, extendedPoint, _maxSpeed * Time.deltaTime);

                    }
                }
            }
            if (closestSheep == null && currentIndex > 4 || currentIndex == 0)
            {
                goToPoint(centerPoint);
                FindClosestSheep();
                currentIndex = 1;

            }
            //default to waypoint system if cannot find a sheep
            if (closestSheep == null && currentIndex == 1)
            {
                goToPoint(wayPoints0);
                FindClosestSheep();
                currentIndex += 1;
            }
            if (closestSheep == null && currentIndex == 2)
            {
                goToPoint(wayPoints1);
                FindClosestSheep();
                currentIndex += 1;
            }
            if (closestSheep == null && currentIndex == 3)
            {
                goToPoint(wayPoints2);
                FindClosestSheep();
                currentIndex += 1;
            }
            if (closestSheep == null && currentIndex == 4)
            {
                goToPoint(wayPoints3);
                FindClosestSheep();
                currentIndex += 1;
            }
        }
    }
}
