using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

public class MGridObject : MonoBehaviour
{
    public GridObjectType type;
    public Vector2Int gridPosition;
    private Vector2Int prevGridPos;
    private Vector2Int initGridPos;
    bool isMoving;

    public Vector2Int GridPosition{
        get=>gridPosition;
        set{
            if(value.x<0||value.y<0||value.x>=GridManager.inst.w||value.y>=GridManager.inst.h)
                return;
            GridManager.inst.gridObjectsMoved=true;
            prevGridPos=gridPosition;
            gridPosition=value;
            UpdatePositionAnim();
        }
    }

    public void Move(int dir){
        if(isMoving) return;
        isMoving=true;
        //construct a hash table
        Dictionary<MGridObject,int> hash=new Dictionary<MGridObject, int>(GridManager.inst.gridObjects.Count);
        //try move
        if(!TryMove(dir, hash)) isMoving=false;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dir">0: up, 1: down, 2: left, 3: right</param>
    /// <returns></returns>
    private bool TryMove(int dir, Dictionary<MGridObject,int> hash){
        if(type==GridObjectType.Wall) return false;
        // 0=false, 1=exploring, 2=true
        if(hash.ContainsKey(this)){
            return hash[this]>0;
        }
        hash.Add(this, 1);

        Vector2Int offset=Vector2Int.zero;
        switch(dir){
            case 0://up
                offset.y=-1;
                break;
            case 1://down
                offset.y=1;
                break;
            case 2://left
                offset.x=-1;
                break;
            case 3://right
                offset.x=1;
                break;
        }
        Vector2Int newPos=gridPosition+offset;
        //itself cannot be outside of the bounds
        if(newPos.x<0||newPos.y<0||newPos.x>=GridManager.inst.w||newPos.y>=GridManager.inst.h){
            hash[this]=0;
            return false;
        }
        MGridObject forwardObj=GridManager.inst.grid[newPos.x,newPos.y];
        //detect forward object
        if(forwardObj!=null){
            switch(forwardObj.type){
                case GridObjectType.Wall:
                    goto case GridObjectType.Clingy;
                case GridObjectType.Clingy:
                    if(hash.ContainsKey(forwardObj) && hash[forwardObj]==2){ 
                        //if the clingy object is already tested and is moving,
                        //then the clingy object shouldn't block the movement of other objects
                        break;
                    }
                    hash[this]=0;
                    return false;
                case GridObjectType.Sticky:
                case GridObjectType.Smooth:
                    if(!forwardObj.TryMove(dir, hash)){
                        hash[this]=0;
                        return false;
                    }
                    break;
            }
        }
        hash[this]=2;
        //------get and detect backward object------
        Vector2Int sidePos=gridPosition-offset;
        MGridObject sideObj=null;
        if(!(sidePos.x<0||sidePos.y<0||sidePos.x>=GridManager.inst.w||sidePos.y>=GridManager.inst.h))
            sideObj=GridManager.inst.grid[sidePos.x,sidePos.y];
        if(sideObj!=null){
            if(type==GridObjectType.Sticky){
                sideObj.TryMove(dir,hash);
            } else{
                switch(sideObj.type){
                    case GridObjectType.Clingy:
                        sideObj.TryMove(dir, hash);
                        break;
                    case GridObjectType.Sticky:
                        sideObj.TryMove(dir, hash);
                        break;
                }
            }
        }
        //----------------------side objects----------------------
        Vector2Int sideOffset=new Vector2Int(-offset.y,offset.x);
        //add all side objects
        List<MGridObject> sideObjs=new List<MGridObject>();
        sidePos=gridPosition-sideOffset;
        if(!(sidePos.x<0||sidePos.y<0||sidePos.x>=GridManager.inst.w||sidePos.y>=GridManager.inst.h) && GridManager.inst.grid[sidePos.x,sidePos.y]!=null)
            sideObjs.Add(GridManager.inst.grid[sidePos.x,sidePos.y]);
        sidePos=gridPosition+sideOffset;
        if(!(sidePos.x<0||sidePos.y<0||sidePos.x>=GridManager.inst.w||sidePos.y>=GridManager.inst.h) && GridManager.inst.grid[sidePos.x,sidePos.y]!=null)
            sideObjs.Add(GridManager.inst.grid[sidePos.x,sidePos.y]);
        //traverse all side objects
        foreach(MGridObject obj in sideObjs){
            //if either (1)this box is stick and the sideObj is not Clingy (which can only be pulled)
            //or (2) the sideObj itself is sticky
            if((type==GridObjectType.Sticky&&obj.type!=GridObjectType.Clingy) || obj.type==GridObjectType.Sticky){
                obj.TryMove(dir, hash);
            }
        }
        GridPosition+=offset;
        return true;
    }
    void Start(){
        UpdatePosition();
        GridManager.inst.RegisterGridObject(this);
        initGridPos=gridPosition;
    }
    void Update(){
        if(Input.GetKeyDown(KeyCode.R)){
            gridPosition=initGridPos;
            UpdatePosition();
        }
    }
    [Button("Update Position")]
    private void UpdatePosition(){
        float x = GridMaker.reference.TopLeft.x + GridMaker.reference.cellWidth * (gridPosition.x + 0.5f); 
        float y = GridMaker.reference.TopLeft.y - GridMaker.reference.cellWidth * (gridPosition.y + 0.5f);
        transform.position=new Vector3(x,y,0);
    }
    static float Grid2WorldPosX(int x){
        return GridMaker.reference.TopLeft.x + GridMaker.reference.cellWidth * (x + 0.5f);
    }
    static float Grid2WorldPosY(int y){
        return GridMaker.reference.TopLeft.y - GridMaker.reference.cellWidth * (y + 0.5f);
    }
    private void UpdatePositionAnim()
    {
        float x = Grid2WorldPosX(gridPosition.x); 
        float y = Grid2WorldPosY(gridPosition.y);
        float oScale=transform.localScale.x; //original scale
        float xscaleAmount=GridManager.inst.player_xscaleAmount*oScale;
        Sequence s=DOTween.Sequence();
        switch(type){
            case GridObjectType.Player:
                if(prevGridPos.x==gridPosition.x){ //vertical move
                    s.Append(transform.DOScaleX(transform.localScale.x-GridManager.inst.scaleAmount, GridManager.inst.moveDuration/2).SetLoops(2,LoopType.Yoyo));
                    if(gridPosition.x>prevGridPos.x){ //move right
                        s.Join(transform.DOMoveY((oScale-xscaleAmount)/2+y, GridManager.inst.moveDuration));
                        s.Join(transform.DOScaleY(xscaleAmount, GridManager.inst.moveDuration));
                        s.Append(transform.DOMoveY(y, GridManager.inst.moveDuration/3));
                        s.Join(transform.DOScaleY(oScale, GridManager.inst.moveDuration/3));
                    } else{
                        s.Join(transform.DOMoveY(-(oScale-xscaleAmount)/2+y, GridManager.inst.moveDuration));
                        s.Join(transform.DOScaleY(xscaleAmount, GridManager.inst.moveDuration));
                        s.Append(transform.DOMoveY(y, GridManager.inst.moveDuration/3));
                        s.Join(transform.DOScaleY(oScale, GridManager.inst.moveDuration/3));
                    }
                }
                else{ //horizontal move
                    s.Append(transform.DOScaleY(transform.localScale.y-GridManager.inst.scaleAmount, GridManager.inst.moveDuration/2).SetLoops(2,LoopType.Yoyo));
                    if(gridPosition.x>prevGridPos.x){ //move right
                        s.Join(transform.DOMoveX((oScale-xscaleAmount)/2+x, GridManager.inst.moveDuration));
                        s.Join(transform.DOScaleX(xscaleAmount, GridManager.inst.moveDuration));
                        s.Append(transform.DOMoveX(x, GridManager.inst.moveDuration/3));
                        s.Join(transform.DOScaleX(oScale, GridManager.inst.moveDuration/3));
                    } else{
                        s.Join(transform.DOMoveX(-(oScale-xscaleAmount)/2+x, GridManager.inst.moveDuration));
                        s.Join(transform.DOScaleX(xscaleAmount, GridManager.inst.moveDuration));
                        s.Append(transform.DOMoveX(x, GridManager.inst.moveDuration/3));
                        s.Join(transform.DOScaleX(oScale, GridManager.inst.moveDuration/3));
                    }
                } 
                break;
            case GridObjectType.Sticky:
                s.Append(transform.DOMove(new Vector3(x,y,0), GridManager.inst.moveDuration));
                s.Join(transform.DOScale(new Vector3(GridManager.inst.sticky_scaleAmount,GridManager.inst.sticky_scaleAmount,1), GridManager.inst.moveDuration/2).SetLoops(2, LoopType.Yoyo));
                break;
            case GridObjectType.Smooth:
                s.Append(transform.DOMove(new Vector3(x,y,0), GridManager.inst.moveDuration));
                if(gridPosition.x>prevGridPos.x || gridPosition.y>prevGridPos.y){ //clockwise
                    s.Join(transform.DORotate(new Vector3(0,0,transform.localRotation.eulerAngles.z-90), GridManager.inst.moveDuration));
                } else{ //counter clockwise
                    s.Join(transform.DORotate(new Vector3(0,0,transform.localRotation.eulerAngles.z+90), GridManager.inst.moveDuration));
                }
                break;
            case GridObjectType.Clingy:
                s.Append(transform.DOMove(new Vector3(x,y,0), GridManager.inst.moveDuration));
                if(gridPosition.x==prevGridPos.x){
                    s.Join(transform.DOScaleY(oScale*2, GridManager.inst.moveDuration/2).SetLoops(2, LoopType.Yoyo));
                    s.Join(transform.DOScaleX(xscaleAmount, GridManager.inst.moveDuration/2).SetLoops(2, LoopType.Yoyo));
                }
                else{
                    s.Join(transform.DOScaleX(oScale*2, GridManager.inst.moveDuration/2).SetLoops(2, LoopType.Yoyo));
                    s.Join(transform.DOScaleY(xscaleAmount, GridManager.inst.moveDuration/2).SetLoops(2, LoopType.Yoyo));
                }
                break;
        }
        s.AppendCallback(()=>isMoving=false);
    }
}
public enum GridObjectType{
    Player,
    Wall,
    Sticky,
    Smooth,
    Clingy,
}