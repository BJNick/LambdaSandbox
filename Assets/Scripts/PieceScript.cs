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
        if (isVariable)
            GetComponentInChildren<TextMeshPro>().text = variableName;
        else if (openingBracket)
            GetComponentInChildren<TextMeshPro>().text = variableName + ".(";
        else if (closingBracket)
            GetComponentInChildren<TextMeshPro>().text = ")";
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
    }

    private void OnCollisionEnter2D(Collision2D other) {
        OnCollisionStay2D(other);
    }

    private void OnCollisionStay2D(Collision2D other) {
        var otherPiece = other.gameObject.GetComponent<PieceScript>();
        if (otherPiece != null) {
            if (otherPiece.inflateProgress < 1) return;
            if (this.inflateProgress < 1) return;
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

}
