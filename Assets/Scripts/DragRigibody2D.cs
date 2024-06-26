﻿/* Copyright (C) 2016 Calvin Sauer - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Calvin Sauer <calvin.j.sauer@gmail.com>, July 2016
 * Version: 0.6
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DragRigidBody2D : MonoBehaviour
{

    //Layers that are affected by dragging
    public LayerMask draggableLayers;

    //How much damping should be applied to dragging. A higher number allows for more sluggish dragging, a lower number allows for snappier dragging
    public float dragDamping = 10.0f;

    //Should the dragged body be allowed to rotate?
    public bool freezeRotation = false;

    //Should we snap the drag to the center of the dragged object?
    public bool snapToCenter = true;

    //If we are snapping to center, how fast should we snap?
    public float snapSpeed = 3.0f;

    //Offset the drag by the velocity of this rigidbody when dragging
    public Rigidbody2D relativeToRigidbody;

    //The prefab that we use for drags	
    GameObject dragPrefab;
    void Start()
    {
        init();

        if (dragDamping == 0)
        {
            Debug.LogWarning("Drag damping is set to 0! This is unsupported behavior. Setting dragDamping to 1...");
            dragDamping = 1.0f;
        }

        //Be careful not to rename the prefab in the editor!
        dragPrefab = Resources.Load("DragRigidbody2DPrefab") as GameObject;
    }

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY

    //Maps touch id to drag info structs
    SortedDictionary<int, DragInfo> drags;
    struct DragInfo
    {
        public Transform draggedTransform;
        public Rigidbody2D draggedRigidbody;
        public HingeJoint2D draggedJoint;

        public Vector3 localPickOffset;
        public RigidbodyConstraints2D oldConstraints;
    }

    void init()
    {
        drags = new SortedDictionary<int, DragInfo>();
    }

    void Update()
    {
        foreach (Touch touch in Input.touches)
        {
            Ray ray = GetComponent<Camera>().ScreenPointToRay(touch.position);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, draggableLayers);

            if (touch.phase == TouchPhase.Began && hit.rigidbody != null && !drags.ContainsKey(touch.fingerId))
            {
                hit.rigidbody.bodyType = RigidbodyType2D.Dynamic;
                //Start building up a profile for this specific drag
                DragInfo info;
                info.draggedTransform = hit.transform;
                info.draggedRigidbody = hit.transform.GetComponent<Rigidbody2D>();
                info.oldConstraints = info.draggedRigidbody.constraints;

                if (freezeRotation)
                    info.draggedRigidbody.constraints = info.draggedRigidbody.constraints | RigidbodyConstraints2D.FreezeRotation;

                //Get the current offset
                info.localPickOffset = hit.transform.InverseTransformPoint(GetComponent<Camera>().ScreenToWorldPoint(touch.position));

                //Instantiate the hinge joint
                GameObject jointTrans = Instantiate(dragPrefab, GetComponent<Camera>().ScreenToWorldPoint(touch.position), info.draggedTransform.rotation) as GameObject;
                info.draggedJoint = jointTrans.GetComponent<HingeJoint2D>();

                //Populate the hinge joint
                info.draggedJoint.anchor = Vector2.zero;
                info.draggedJoint.connectedAnchor = info.localPickOffset;
                info.draggedJoint.connectedBody = info.draggedRigidbody;

                //Add to the dictionary
                drags.Add(touch.fingerId, info);

                //Let the GameObject know that we started dragging it
                info.draggedTransform.gameObject.SendMessage("OnStartDrag", SendMessageOptions.DontRequireReceiver);
            }

            //Stop dragging
            if (touch.phase == TouchPhase.Ended && drags.ContainsKey(touch.fingerId))
            {

                hit.rigidbody.bodyType = RigidbodyType2D.Static;
                DragInfo info = drags[touch.fingerId];
                
                //reset the fixed angle value
                if (info.draggedRigidbody != null)
                {
                    info.draggedRigidbody.constraints = info.oldConstraints;
                }

                //destroy the dragging prefab
                if (info.draggedJoint != null)
                {
                    Destroy(info.draggedJoint.gameObject);
                }

                //Let the GameObject know we've stopped dragging it
                if (info.draggedTransform != null)
                {
                    info.draggedTransform.gameObject.SendMessage("OnStopDrag", SendMessageOptions.DontRequireReceiver);
                }

                //Remove from drags dictionary
                drags.Remove(touch.fingerId);
            }
        }
    }

    void FixedUpdate()
    {
        // Placing your first finger on the screen is designated touch 0 by Unity's Input manager.
        // The second finger you place on the screen is designated touch 1, and so on and so forth.
        // When you remove touch 0, touch 1 still remains as touch 1; touch IDs are persistent.
        // HOWEVER, you can NOT look it up with Input.GetTouch(1), because the Input.touches array is indexed from 0.
        // To get around this, we consider every drag in order (they're sorted by touch ID), 
        //   and we always look up touches with Input.GetTouch starting from 0.
        // In the above example, touch ID 1 will actually be looked up as Input.GetTouch(0) because it is the only touch.
        // If we were to place another finger down, that finger would pick up the smallest availabe touch ID, which is 0.
        // Because we always consider drags in sorted order, the drags and their associated finger are always kept in sync.
        int i = 0;
        foreach (var entry in drags)
        {
            DragInfo info = drags[entry.Key];
            Touch touch = Input.GetTouch(i++);

            //Drag
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Vector3 touchCoords = touch.position;

                //Lerp to the snap position
                if (snapToCenter)
                    info.draggedJoint.connectedAnchor = Vector2.Lerp(info.draggedJoint.connectedAnchor, Vector2.zero, snapSpeed * Time.deltaTime);

                Vector3 objectCoords = GetComponent<Camera>().WorldToScreenPoint(info.draggedJoint.transform.position);
                float distance = Vector2.Distance(objectCoords, touchCoords);
                Vector2 vector = (touchCoords - objectCoords).normalized * (distance / dragDamping);
                if (relativeToRigidbody != null)
                    vector += relativeToRigidbody.velocity;
                info.draggedJoint.GetComponent<Rigidbody2D>().velocity = vector;
            }
            //else
            //{
            //    info.draggedJoint.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            //    info.draggedJoint.GetComponent<Rigidbody2D>().angularVelocity = 0;
            //}
        }
    }

#endif

#if UNITY_STANDALONE || UNITY_WEBPLAYER

    Transform currentlyDraggedTransform;
    Rigidbody2D currentlyDraggedRigidbody;
    HingeJoint2D currentlyDraggedJoint;

    Vector3 localPickOffset;

    RigidbodyConstraints2D oldConstraints;

    void init()
    {
        //empty
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //See if we hit any draggables
            Ray ray = GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, draggableLayers);

            if (hit.rigidbody != null)
            {
                //save some drag info
                currentlyDraggedTransform = hit.transform;
                currentlyDraggedRigidbody = hit.transform.GetComponent<Rigidbody2D>();
                oldConstraints = currentlyDraggedRigidbody.constraints;

                if (freezeRotation)
                    currentlyDraggedRigidbody.constraints = currentlyDraggedRigidbody.constraints | RigidbodyConstraints2D.FreezeRotation;

                //Get the current offset
                localPickOffset = hit.transform.InverseTransformPoint(GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition));

                //Instantiate the hinge joint
                GameObject jointTrans = Instantiate(dragPrefab, GetComponent<Camera>().ScreenToWorldPoint(Input.mousePosition), currentlyDraggedTransform.rotation) as GameObject;
                currentlyDraggedJoint = jointTrans.GetComponent<HingeJoint2D>();

                //Populate the hinge joint
                currentlyDraggedJoint.anchor = Vector2.zero;
                currentlyDraggedJoint.connectedAnchor = localPickOffset;
                currentlyDraggedJoint.connectedBody = currentlyDraggedRigidbody;

                //Let the object know that we've started dragging it
                currentlyDraggedTransform.gameObject.SendMessage("OnStartDrag", SendMessageOptions.DontRequireReceiver);
            }
        }

        //Stop dragging
        if (Input.GetMouseButtonUp(0))
        {
            if (currentlyDraggedTransform != null)
            {
                //Let the object know that we have stopped dragging it
                currentlyDraggedTransform.gameObject.SendMessage("OnStopDrag", SendMessageOptions.DontRequireReceiver);
                currentlyDraggedTransform = null;
            }

            if (currentlyDraggedRigidbody != null)
            {
                currentlyDraggedRigidbody.constraints = oldConstraints;
                currentlyDraggedRigidbody = null;
            }

            if (currentlyDraggedJoint != null)
            {
                Destroy(currentlyDraggedJoint.gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        //Drag while the mouse is held down
        if (Input.GetMouseButton(0))
        {
            if (currentlyDraggedTransform != null && currentlyDraggedRigidbody != null && currentlyDraggedJoint != null)
            {
                if (snapToCenter)
                    currentlyDraggedJoint.connectedAnchor = Vector2.Lerp(currentlyDraggedJoint.connectedAnchor, Vector2.zero, snapSpeed * Time.deltaTime);
                Vector3 mouseCoords = Input.mousePosition;
                Vector3 objectCoords = GetComponent<Camera>().WorldToScreenPoint(currentlyDraggedJoint.transform.position);
                float distance = Vector2.Distance(objectCoords, mouseCoords);
                Vector2 vector = (mouseCoords - objectCoords).normalized * (distance / dragDamping);
                if (relativeToRigidbody != null)
                    vector += relativeToRigidbody.velocity;
                currentlyDraggedJoint.GetComponent<Rigidbody2D>().velocity = vector;
            }
        }
    }

#endif
}