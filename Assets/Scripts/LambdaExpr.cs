using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LambdaExpr : MonoBehaviour
{
    // Children of this object are PieceScript objects
    // which are clauses of a lambda expression

    public PieceScript openingBracket;
    public PieceScript closingBracket;

    public string variableName = "x";

    public GameObject toReplaceWith;

    public bool testReplace = false;

    public bool testUnpack = false;

    public void ReplacePiecesWith(string specificVariable, GameObject obj) {
        // Replace all pieces with variableName with object
        List<GameObject> toDestroy = new List<GameObject>();
        List<GameObject> toInstantiate = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);
            var piece = child.GetComponent<PieceScript>();
            if (piece != null) {
                if (piece.variableName == specificVariable && piece.isVariable) {
                    toDestroy.Add(child.gameObject);
                    toInstantiate.Add(obj);
                }
            }
            var lambda = child.GetComponent<LambdaExpr>();
            if (lambda != null) {
                Debug.Log("Found lambda expr recursively");
                lambda.ReplacePiecesWith(specificVariable, obj);
            }
        }
        for (int i = 0; i < toDestroy.Count; i++) {
            // instantiate at the same position as destroyed object
            var pos = toDestroy[i].transform.position;
            var newObj = Instantiate(toInstantiate[i], pos, Quaternion.identity, transform);
            // For all children in new object, set their position to pos + j
            // TODO: Make it recursive
            for (int j = 0; j < newObj.transform.childCount; j++) {
                var child = newObj.transform.GetChild(j);
                child.transform.position = pos + child.transform.localPosition*0.01f;
            }
            Destroy(toDestroy[i]);
        }
    }

    public void Unpack() {
        // Removes the starting and ending bracket from this expression
        openingBracket.gameObject.SetActive(false);
        closingBracket.gameObject.SetActive(false);
    }
    public PieceScript GetOpeningBracket() {
        return openingBracket;
    }

    public PieceScript GetClosingBracket() {
        return closingBracket;
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (testReplace) {
            testReplace = false;
            ReplacePiecesWith(this.variableName, toReplaceWith);
        }
        if (testUnpack) {
            testUnpack = false;
            Unpack();
        }
    }
}
