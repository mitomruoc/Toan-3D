using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Drag : MonoBehaviour, IBeginDragHandler,IDragHandler,IEndDragHandler
{
    private Vector2 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = (Input.mousePosition);
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.position = startPos;
    }
}
