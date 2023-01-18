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
    public bool testCollapse = false;

    public bool remoteBody = false;
    public GameObject remoteBodySource = null;

    bool remoteBodyInstantiated = false;

    public float extraWaitForLong = 1;

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
            var lambda = newObj.GetComponent<LambdaExpr>();
            if (lambda != null) {
                lambda.CollapseTest(0.1f, pos);
            } else {
                newObj.transform.position = pos;    
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

    public float CollapseTest(float factor, Vector3 newPos) {
        var bracketPos = openingBracket.transform.position;
        float totalLength = 0;
        for (int i = 0; i < transform.childCount; i++) {
            var child = transform.GetChild(i);
            var piece = child.GetComponent<PieceScript>();
            if (piece != null){
                var diff = child.position - bracketPos;
                var resultPos = newPos + new Vector3(diff.x*factor, diff.y, diff.z);
                child.position = resultPos;
                totalLength += 1 * factor;
                piece.Reinflate(transform.childCount * extraWaitForLong);
                // child.GetComponent<Rigidbody2D>().MovePosition(resultPos);  // DEFINITELY NOT WORKING
            }
            var lambda = child.GetComponent<LambdaExpr>();
            if (lambda != null) {
                var localPos = lambda.openingBracket.gameObject.transform.position - bracketPos;
                totalLength += lambda.CollapseTest(factor, newPos + new Vector3(localPos.x*factor, localPos.y, localPos.z));
            }
        }
        return totalLength;
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
            var ray = new Ray2D(new Vector2(-100, remoteBodySource.transform.position.y), Vector2.right);
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
            var anchor = new Vector3(0, remoteBodySource.transform.position.y, 0);
            var exprPos = openingBracket.transform.position;
            foreach (var obj in toInstantiate) {
                var pos = obj.transform.position;
                var newObj = Instantiate(obj, exprPos, Quaternion.identity, transform.parent);
                var lambda = newObj.GetComponent<LambdaExpr>();
                if (lambda != null) {
                    var innerOpeningBracket = obj.GetComponent<LambdaExpr>().openingBracket.gameObject.transform.position;
                    var diff = innerOpeningBracket - anchor;
                    lambda.CollapseTest(0.1f,  exprPos + new Vector3(diff.x*0.1f, diff.y, diff.z));
                } else {
                    var diff = obj.transform.position - anchor;
                    newObj.transform.position = exprPos + diff*0.1f;
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

    void FixedUpdate()
    {
        if (testReplace) {
            testReplace = false;
            ReplacePiecesWith(this.variableName, toReplaceWith);
        }
        if (testUnpack) {
            testUnpack = false;
            Unpack();
        }
        if (testCollapse) {
            testCollapse = false;
            CollapseTest(0.1f, new Vector3(-1, 1.5f, 0));
        }
    }
}
