using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GhostNode : MonoBehaviour
{
    Camera cam;
    Grid grid;
    MainManager mm;
    // Use this for initialization
    void Start()
    {
        var ui = UIReferences.Instance;
        cam = ui.camSence;
        grid = ui.grid;
        mm = GameObject.Find(Constants.GameObjectNames.MainManager).GetComponent<MainManager>();
    }

    // Update is called once per frame
    void Update()
    {
        var pos = grid.WorldToCell(cam.ScreenToWorldPoint(Input.mousePosition));
        transform.position = grid.CellToWorld(new Vector3Int(pos.x, pos.y, 0));
        if (!EventSystem.current.currentSelectedGameObject && Input.GetMouseButtonDown(0))
        {
            mm.NewNode(pos);
        }
        if (!EventSystem.current.currentSelectedGameObject && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            Destroy(gameObject);
        }
    }
}
