using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class firebullet1 : MonoBehaviour
{
    public float speed = 10f;
    public Rigidbody2D rb;

    private bool isFacingRight = true; // Karakterin sağa dönük olduğunu takip eden bir flag

    void Start()
    {
        rb.velocity = (isFacingRight ? transform.right : -transform.right) * speed;
    }

    public void SetDirection(bool isRight)
    {
        isFacingRight = isRight;
    }
}
