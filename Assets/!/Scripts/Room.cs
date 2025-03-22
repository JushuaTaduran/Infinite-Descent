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

    public GameObject topWall;    // Reference to the top wall
    public GameObject bottomWall; // Reference to the bottom wall
    public GameObject leftWall;   // Reference to the left wall
    public GameObject rightWall;  // Reference to the right wall

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
            if (topWall != null) topWall.SetActive(false); // Close the top wall
        }

        if (direction == Vector2Int.down && bottomDoor != null)
        {
            bottomDoor.SetActive(true);
            if (bottomWall != null) bottomWall.SetActive(false); // Close the bottom wall
        }

        if (direction == Vector2Int.left && leftDoor != null)
        {
            leftDoor.SetActive(true);
            if (leftWall != null) leftWall.SetActive(false); // Close the left wall
        }

        if (direction == Vector2Int.right && rightDoor != null)
        {
            rightDoor.SetActive(true);
            if (rightWall != null) rightWall.SetActive(false); // Close the right wall
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