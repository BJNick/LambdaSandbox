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

    public bool grabbable = true;

    public bool doNotOverrideText = false;

    bool triggerOverlaps = false;

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

    public GameObject GetOutermostParentTransform() {
        var parent = transform;
        while (parent.parent != null) {
            parent = parent.parent;
        }
        return parent.gameObject;
    }

    // Start is called before the first frame update
    void Start()
    {
        Reinflate();
        if (doNotOverrideText) {
            return;
        }
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

    public void Reinflate(float waitTime = -1) {
        if (waitTime < waitOnStart) waitTime = waitOnStart;
        inflateProgress = 0;
        waiting = true;
        CancelInvoke("StopWaiting");
        Invoke("StopWaiting", waitOnStart);
        GetComponent<Rigidbody2D>().velocity = Vector3.zero;
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

        /* var circleCol = GetComponentInChildren<CircleCollider2D>();
        triggerOverlaps = circleCol.OverlapCollider(new ContactFilter2D(), new Collider2D[2]) > 1;
        if (triggerOverlaps) Debug.Log("Trigger overlaps"); */

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

        if (noLongerCarried) {
            // Update context for all pieces in the scene
            var pieces = GameObject.FindObjectsOfType<PieceScript>();
            foreach (var piece in pieces) {
                piece.UpdatePieceContext();
            }
        }
    }

    void UpdatePieceContext() {
        if (!isVariable && !openingBracket) return;
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

    void TriggerSubstitution(LambdaExpr myExpr, GameObject fullOtherPiece) {
        if (!myExpr.IsLeftmostExpression()) return;
        if (!myExpr.IsBodyInstantiated()) {
            myExpr.InstantiateBody();
            return;
        }
        
        myExpr.ReplacePiecesWith(GetParentExpr().variableName, fullOtherPiece.gameObject);
        myExpr.Unpack();
        fullOtherPiece.SetActive(false);
    }

    void ClosingBracketCheck(Collision2D other) {
        if (!closingBracket) return;

        var otherPiece = other.gameObject.GetComponent<PieceScript>();
        if (otherPiece != null) {
            if (otherPiece.waiting) return;
            if (this.waiting) return;
            if (this.triggerOverlaps) return;
            if (otherPiece.triggerOverlaps) return;
            if (otherPiece.closingBracket && !otherPiece.openingBracket) 
                return; // to let remote body pieces to work
            
            if (otherPiece.transform.position.x > transform.position.x) {   
                Debug.Log("Closing bracket trigger hit");
                var fullOtherPiece = otherPiece.openingBracket ? 
                    otherPiece.GetParentOrSelf() : otherPiece.gameObject;
                var myExpr = GetParentExpr();
                if (myExpr != null && myExpr.gameObject != fullOtherPiece) {
                    TriggerSubstitution(myExpr, fullOtherPiece);
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other) {
        ClosingBracketCheck(other);
    }

    private void OnCollisionStay2D(Collision2D other) {
        ClosingBracketCheck(other);
    }

    private void OnMouseOver() {
        // If mouse held, move piece
        if (Input.GetMouseButtonDown(0) && grabbable) {
            isCarried = true;
        }
    }

}
