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

    public bool remoteBody = false;
    public float remoteBodyY = 0;

    bool remoteBodyInstantiated = false;

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
                if (child.GetComponent<LambdaExpr>() != null) {
                    child.GetComponent<LambdaExpr>().CollapseChildren(0.01f, pos);
                }
            }
            Destroy(toDestroy[i]);
        }
    }

    public void CollapseChildren(float factor, Vector3 initPos) {
        var myOldPos = transform.position;
        for (int i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);
            var piece = child.GetComponent<PieceScript>();
            if (piece != null)
                child.gameObject.transform.position = initPos + Vector3.right*piece.transform.localPosition.x*factor;
            var lambda = child.GetComponent<LambdaExpr>();
            if (lambda != null) {
                var localPos = lambda.openingBracket.gameObject.transform.position - myOldPos;
                lambda.CollapseChildren(factor * 0.1f, initPos + Vector3.right*localPos.x*factor);
            }
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

    public bool IsLeftmostExpression() {
        var ray = new Ray2D(openingBracket.transform.position, Vector2.left);
        var hit = Physics2D.Raycast(ray.origin + Vector2.left, ray.direction, 100, LayerMask.GetMask("Opening Bracket"));
        if (hit.collider != null) {
            Debug.Log("Hit " + hit.rigidbody.gameObject.name + " so not leftmost");
            return false;
        }
        return true;
    }

    public bool IsBodyInstantiated() {
        return !remoteBody || remoteBodyInstantiated;
    }

    public void InstantiateBody() {
        if (remoteBody && !remoteBodyInstantiated) {
            Debug.Log("Instantiating body");
            // Raycast entire X axis on the coordinate to find the bodies to instantiate
            var ray = new Ray2D(new Vector2(-100, remoteBodyY), Vector2.right);
            var hits = Physics2D.RaycastAll(ray.origin, ray.direction, 200, LayerMask.GetMask("Piece", "Opening Bracket"));
            HashSet<GameObject> toInstantiate = new HashSet<GameObject>();
            foreach (var hit in hits) {
                // Get the piece or its parent
                var piece = hit.rigidbody.GetComponent<PieceScript>();
                if (piece != null) {
                    Debug.Log("Adding piece " + piece.name);
                    toInstantiate.Add(piece.GetOutermostParentTransform());
                }
            }
            foreach (var obj in toInstantiate) {
                var pos = obj.transform.position;
                var newObj = Instantiate(obj, openingBracket.transform.position, Quaternion.identity, transform.parent);
                for (int i = 0; i < newObj.transform.childCount; i++) {
                    var child = newObj.transform.GetChild(i);
                    child.transform.position = openingBracket.transform.position + Vector3.right*child.transform.position.x*0.1f;
                    if (child.GetComponent<LambdaExpr>() != null) {
                        child.GetComponent<LambdaExpr>().CollapseChildren(0.01f, openingBracket.transform.position);
                    }
                }
            }
            remoteBodyInstantiated = true;
            openingBracket.gameObject.SetActive(false);
        }
    }

    void Awake()
    {
        // Set opening and closing variable to match this object
        openingBracket.variableName = variableName;
        closingBracket.variableName = variableName;
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
