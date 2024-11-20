using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public float moveDuration, scaleAmount;
    public float player_xscaleAmount, sticky_scaleAmount;

    public static GridManager inst;
    [HideInInspector] public int w,h;
    [HideInInspector] public MGridObject[,] grid;
    [HideInInspector] public List<MGridObject> gridObjects;
    [HideInInspector] public bool gridObjectsMoved;

    void Awake(){
        inst=this;
        w=(int)GridMaker.reference.dimensions.x;
        h=(int)GridMaker.reference.dimensions.y;
        grid=new MGridObject[w,h];
        gridObjects=new List<MGridObject>();
    }
    public void RegisterGridObject(MGridObject go){
        gridObjects.Add(go);
        grid[go.gridPosition.x,go.gridPosition.y]=go;
    }
    public void UpdateGridMap(){
        for(int i=0;i<w;++i){
            for(int j=0;j<h;++j){
                grid[i,j]=null;
            }
        }
        foreach(MGridObject m in gridObjects){
            grid[m.gridPosition.x,m.gridPosition.y]=m;
        }
    }
    void FixedUpdate(){
        if(gridObjectsMoved){
            UpdateGridMap();
            gridObjectsMoved=false;
        }
    }
}
