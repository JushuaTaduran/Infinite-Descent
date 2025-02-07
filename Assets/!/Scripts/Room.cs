using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public Vector2Int RoomIndex { get; set; }

    public GameObject topDoor;
    public GameObject bottomDoor;
    public GameObject leftDoor;
    public GameObject rightDoor;

    public GameObject verticalRoom;
    public GameObject horizontalRoom;
    public GameObject ltRoom;
    public GameObject rtRoom;
    public GameObject lbRoom;
    public GameObject rbRoom;

    public void OpenDoor(Vector2Int direction)
    {
        if (direction == Vector2Int.up && topDoor != null)
        {
            topDoor.SetActive(true);
        }

        if (direction == Vector2Int.down && bottomDoor != null)
        {
            bottomDoor.SetActive(true);
        }

        if (direction == Vector2Int.left && leftDoor != null)
        {
            leftDoor.SetActive(true);
        }

        if (direction == Vector2Int.right && rightDoor != null)
        {
            rightDoor.SetActive(true);
        }
    }

    public void ActivateRoomType(string roomType)
    {
        if (verticalRoom != null) verticalRoom.SetActive(roomType == "VerticalRoom");
        if (horizontalRoom != null) horizontalRoom.SetActive(roomType == "HorizontalRoom");
        if (ltRoom != null) ltRoom.SetActive(roomType == "LTRoom");
        if (rtRoom != null) rtRoom.SetActive(roomType == "RTRoom");
        if (lbRoom != null) lbRoom.SetActive(roomType == "LBRoom");
        if (rbRoom != null) rbRoom.SetActive(roomType == "RBRoom");
    }
}