using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lineAnchorManager : MonoBehaviour
{
    public GameObject anchorPrefab;
    LineRenderer lr;
    List<GameObject> listAnchor;
    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
        updateAnchor();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void updateAnchor()
    {
        if (lr.positionCount > 2)
        {
            Vector3[] x = new Vector3[lr.positionCount];
            lr.GetPositions(x);
            for (int i = 1; i < x.Length - 1; i++)
            {
                GameObject a = Instantiate(anchorPrefab, x[i] + Vector3.back * 2, new(), transform);
                a.name = $"{i}";
            }

        }
    }
}
