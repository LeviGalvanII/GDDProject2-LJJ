using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PlayerController : MonoBehaviour
{
     #region Movement_variables
    public float moveSpeed = 3;
    float x_input;
    float y_input;
    #endregion
    Vector2 currDirection;
    public bool grounded;
    public LayerMask groundLayer;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Rigidbody2D PlayerRB;
    public BoxCollider2D boxCollider;
    private void Awake()
    {

        PlayerRB = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        x_input = Input.GetAxisRaw("Horizontal");
        y_input = Input.GetAxisRaw("Vertical");
        Move();
        if (Input.GetKey(KeyCode.W) && isGrounded() == true)
        {
            Jump();
        }
    }



    private void OnCollisionEnter2D(Collision2D collision)
    {
    
    }

    private bool isGrounded()
    {
        RaycastHit2D raycastHit = Physics2D.BoxCast(boxCollider.bounds.center,boxCollider.bounds.size,0,Vector2.down,0.1f,groundLayer);
        return raycastHit.collider!=null;
    }

    private void Jump()
    {
        PlayerRB.linearVelocity = new Vector2(PlayerRB.linearVelocity.x, moveSpeed);
    
    }

    private void Move()
    {
        PlayerRB.linearVelocity = new Vector2(x_input * moveSpeed, PlayerRB.linearVelocity.y);
       
        //anim.SetBool("Moving", true);
        /*TODO 1.1: Edit the Move() function which will set PlayerRB.velocity to a vector based on which input the player is pressing.*/
        // if (x_input > 0)
        // {
        //     PlayerRB.linearVelocity = Vector2.right * moveSpeed;
        //     currDirection = Vector2.right;

        // }
        // else if (x_input < 0)
        // {
        //     PlayerRB.linearVelocity = Vector2.left * moveSpeed;
        //     currDirection = Vector2.left;
        // }
        // else if (y_input > 0)
        // {
        //     PlayerRB.linearVelocity = Vector2.up * moveSpeed;
        //     currDirection = Vector2.up;
        // }
        // else if (y_input < 0)
        // {
        //     PlayerRB.linearVelocity = Vector2.down * moveSpeed;
        //     currDirection = Vector2.down;
        // }
        // else
        // {
        //     PlayerRB.linearVelocity = Vector2.zero;
        //     // anim.SetBool("Moving", false);
        // }
        /* TODO 1.4: Set currDirection to the correct Vector direction i.e. Vector2.left.
             * HINT: there are four cardinal directions. */


        /* DO NOT MODIFY ANYTHING BELOW THIS LINE UNLESS YOU REALLY KNOW WHAT YOU'RE DOING */

        //    // if (x_input == 0 && y_input == 0)
        //     {
        //         anim.SetBool("Moving", false);
        //     }
        //     else
        //     {
        //         //anim.SetBool("Moving", true);

        //     }

        // anim.SetFloat("DirX", currDirection.x);
        //anim.SetFloat("DirY", currDirection.y);

    }
}

