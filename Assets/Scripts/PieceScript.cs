using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PieceScript : MonoBehaviour
{

    public bool openingBracket = false;
    public bool closingBracket = false;

    public bool isVariable = false;
    public string variableName = "x";


    public bool inflate = true;

    public float inflateSpeed = 1;

    float inflateProgress = 0;

    public float lineSeparation = 1.5f;

    bool isCarried = false;

    public float waitOnStart = 5;
    bool waiting = true;

    public LambdaExpr GetParentExpr() {
        var parent = transform.parent;
        while (parent != null) {
            var lambda = parent.GetComponent<LambdaExpr>();
            if (lambda != null) {
                return lambda;
            }
            parent = parent.parent;
        }
        return null;
    }

    public GameObject GetParentOrSelf() {
        var parent = GetParentExpr();
        if (parent == null) {
            return gameObject;
        }
        return parent.gameObject;
    }

    // Start is called before the first frame update
    void Start()
    {
        inflateProgress = 0;
        waiting = true;
        Invoke("StopWaiting", waitOnStart);
        if (isVariable)
            GetComponentInChildren<TextMeshPro>().text = variableName;
        else if (openingBracket)
            GetComponentInChildren<TextMeshPro>().text = variableName + ".(";
        else if (closingBracket)
            GetComponentInChildren<TextMeshPro>().text = ")";
    }

    void StopWaiting() {
        waiting = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (inflate) {
            transform.localScale = new Vector3(inflateProgress, 1, 1);
            if (inflateProgress < 1) {
                inflateProgress += Time.deltaTime * inflateSpeed;
            } else {
                inflateProgress = 1;
            }
        }

        bool noLongerCarried = false;

        if (isCarried) {
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var newPos = new Vector3(mousePos.x, Mathf.Round(mousePos.y/lineSeparation)*lineSeparation, 0);
            transform.position = newPos;
            // Disable collider
            GetComponent<Rigidbody2D>().isKinematic = true;
            GetComponentInChildren<Collider2D>().enabled = false;
            if (!Input.GetMouseButton(0)) {
                isCarried = false;
                noLongerCarried = true;
                //Start();
            }
        } else {
            // Enable collider
            GetComponent<Rigidbody2D>().isKinematic = false;
            GetComponentInChildren<Collider2D>().enabled = true;
        }

        if ((isVariable || openingBracket) && noLongerCarried) {
            // Raycast to the left to find opening bracket
            var ray = new Ray2D(transform.position, Vector2.left);
            var hits = Physics2D.RaycastAll(ray.origin, ray.direction, 100, LayerMask.GetMask("Opening Bracket"));
            // find the closest one with closing bracket to the right of this body
            PieceScript closestClosingBracket = null;
            for (int i = 0; i < hits.Length; i++) {
                var hit = hits[i];
                var hitPiece = hit.rigidbody.GetComponent<PieceScript>();
                var hitExpr = hitPiece.GetParentExpr();
                var closingBracket = hitExpr.GetClosingBracket();
                if (closingBracket != null) {
                    var closingBracketPos = closingBracket.transform.position;
                    if (closingBracketPos.x > transform.position.x) {
                        if (closestClosingBracket == null || 
                            closingBracketPos.x < closestClosingBracket.transform.position.x) {
                            closestClosingBracket = hitPiece;
                        }
                    }
                }
            }
            // make the closestClosingBracket the parent of this body
            if (closestClosingBracket != null) {
                var closestClosingBracketExpr = closestClosingBracket.GetParentExpr();
                if (closestClosingBracketExpr != null) {
                    if (isVariable) {
                        transform.SetParent(closestClosingBracketExpr.transform);
                    } else if (openingBracket){
                        GetParentExpr().transform.SetParent(closestClosingBracketExpr.transform);
                        Debug.Log("Parenting to " + closestClosingBracketExpr.gameObject.name);
                    }
                }
            } else {
                if (isVariable) {
                    transform.SetParent(null);
                } else if (openingBracket) {
                    GetParentExpr().transform.SetParent(null);
                    Debug.Log("Parenting to null");
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other) {
        OnCollisionStay2D(other);
    }

    private void OnCollisionStay2D(Collision2D other) {
        var otherPiece = other.gameObject.GetComponent<PieceScript>();
        if (otherPiece != null) {
            if (otherPiece.waiting) return;
            if (this.waiting) return;
            if (otherPiece.closingBracket) return;
            
            if (closingBracket && otherPiece.transform.position.x > transform.position.x) {   
                Debug.Log("Closing bracket hit");
                var fullOtherPiece = otherPiece.openingBracket ? 
                    otherPiece.GetParentOrSelf() : otherPiece.gameObject;
                var myExpr = GetParentExpr();
                if (myExpr != null && myExpr.gameObject != fullOtherPiece) {
                    myExpr.ReplacePiecesWith(GetParentExpr().variableName, fullOtherPiece.gameObject);
                    myExpr.Unpack();
                    fullOtherPiece.SetActive(false);
                }
            }
        }
    }

    private void OnMouseOver() {
        // If mouse held, move piece
        if (Input.GetMouseButtonDown(0)) {
            isCarried = true;
        }
    }

}
