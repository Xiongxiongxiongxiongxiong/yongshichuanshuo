using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Temporary B-key appearance test. Original art remains the default equipment.</summary>
[DefaultExecutionOrder(2000)]
public sealed class TestWeaponSwap : MonoBehaviour
{
    [System.Serializable]
    public sealed class WeaponFrame
    {
        public Sprite source;
        public Sprite sprite;
        public Vector2 hand;
        public bool hidden;
        public int sortingOffset = 1;
        public Rect bodyRect;
        public Vector2 bodyPivot;
        public bool cropBody;
        public bool flipVertical;
        public bool flipHorizontal;
    }
    [Header("Frame-locked weapon animation (preferred)")]
    public WeaponFrame[] animatedFrames;
    public Texture2D cleanMain, cleanExtra;
    public Sprite weapon;
    [Header("Missing default idle sword")]
    public Sprite defaultIdleWeapon;
    public float defaultIdleAngle = 15f;
    public Sprite[] originalFrames;
    public bool temporaryBKey = true;
    public bool IsNewWeapon { get; private set; }
    private SpriteRenderer body, equipped;
    private PlayerController controller;
    private readonly Dictionary<Sprite, Sprite> clean = new Dictionary<Sprite, Sprite>();
    private readonly Dictionary<Sprite, Sprite> originals = new Dictionary<Sprite, Sprite>();
    private readonly Dictionary<Sprite, WeaponFrame> animation = new Dictionary<Sprite, WeaponFrame>();
    public Sprite CurrentWeaponFrame => equipped != null && equipped.enabled ? equipped.sprite : null;
    public WeaponFrame CurrentBinding { get; private set; }

    // Equipment shares the character's sprite clock and the existing calibrated body/hand anchors.
    public bool EquipFrames(WeaponFrame[] frames)
    {
        if (frames == null || frames.Length != originalFrames.Length) return false;
        var checkedFrames = new Dictionary<Sprite, WeaponFrame>();
        foreach (var frame in frames)
        {
            if (frame == null || frame.source == null || (!frame.hidden && frame.sprite == null) ||
                checkedFrames.ContainsKey(frame.source)) return false;
            checkedFrames.Add(frame.source, frame);
        }
        foreach (var source in originalFrames) if (!checkedFrames.ContainsKey(source)) return false;
        animation.Clear();
        foreach (var entry in checkedFrames) animation.Add(entry.Key, entry.Value);
        IsNewWeapon = true;
        return true;
    }

    public void RestoreDefaultWeapon()
    {
        animation.Clear();
        if (animatedFrames != null) foreach (var frame in animatedFrames)
            if (frame != null && frame.source != null) animation[frame.source] = frame;
        IsNewWeapon = false;
        CurrentBinding = null;
    }
    private static readonly Vector2[] AttackGrips={new Vector2(.22f,.50f),new Vector2(.18f,.65f),new Vector2(.35f,.62f),new Vector2(.58f,.38f),new Vector2(.64f,.36f),new Vector2(.52f,.36f),new Vector2(.40f,.24f),new Vector2(.40f,.30f)};
    private static readonly float[] AttackAngles={15,30,50,-80,-90,-80,-100,-50};

    private void Awake()
    {
        body = GetComponent<SpriteRenderer>(); controller = GetComponent<PlayerController>();
        var child = new GameObject("Independent Test Sword");child.transform.SetParent(transform,false);
        equipped = child.AddComponent<SpriteRenderer>();equipped.sprite=weapon;equipped.enabled=false;
        if(animatedFrames!=null) foreach(var frame in animatedFrames)
            if(frame!=null&&frame.source!=null)animation[frame.source]=frame;
        foreach(var frame in originalFrames)
        {
            if(frame==null||clean.ContainsKey(frame))continue;
            bool extra=frame.name.StartsWith("RedKnightSkill_");
            var texture=extra?cleanExtra:cleanMain;
            Vector2 scale=new Vector2((float)texture.width/frame.texture.width,(float)texture.height/frame.texture.height);
            Rect r=frame.rect;
            WeaponFrame bodyPose;animation.TryGetValue(frame,out bodyPose);
            Rect bodyRect=bodyPose!=null&&bodyPose.cropBody?bodyPose.bodyRect:new Rect(r.x*scale.x,r.y*scale.y,r.width*scale.x,r.height*scale.y);
            Vector2 bodyPivot=bodyPose!=null&&bodyPose.cropBody?bodyPose.bodyPivot:new Vector2(frame.pivot.x/r.width,frame.pivot.y/r.height);
            var replacement=Sprite.Create(texture,bodyRect,bodyPivot,frame.pixelsPerUnit*scale.y,0,SpriteMeshType.FullRect);
            replacement.name=frame.name+"_Weaponless";clean.Add(frame,replacement);originals.Add(replacement,frame);
        }
    }
    private void Update()
    {
        if(temporaryBKey&&Keyboard.current!=null&&Keyboard.current.bKey.wasPressedThisFrame&&
            Time.timeScale>0&&!controller.isDead&&controller.inputcontroller!=null&&controller.inputcontroller.GamePlayer.Move.enabled)
            SwitchWeapon();
    }
    public void SwitchWeapon() { IsNewWeapon=!IsNewWeapon; }
    private void LateUpdate()
    {
        CurrentBinding = null;
        if(body==null||body.sprite==null)return;
        Sprite original;
        if(!originals.TryGetValue(body.sprite,out original))original=body.sprite;
        if(!IsNewWeapon)
        {
            body.sprite=original;
            WeaponFrame idlePose;
            int idleIndex;
            bool idle=original.name.StartsWith("RedKnight_")&&int.TryParse(original.name.Substring(10),out idleIndex)&&idleIndex<6;
            if(idle&&defaultIdleWeapon!=null&&animation.TryGetValue(original,out idlePose)&&!controller.isDead&&!controller.IsClimbingLadder)
            {
                float idleFacing=body.flipX?-1:1;
                equipped.sprite=defaultIdleWeapon;
                equipped.transform.localPosition=new Vector3(idlePose.hand.x*idleFacing,idlePose.hand.y,0);
                equipped.transform.localRotation=Quaternion.Euler(0,0,defaultIdleAngle*idleFacing);
                equipped.transform.localScale=Vector3.one;
                equipped.flipX=body.flipX;equipped.flipY=false;
                equipped.sortingLayerID=body.sortingLayerID;equipped.sortingOrder=body.sortingOrder-1;
                equipped.color=body.color;equipped.sharedMaterial=body.sharedMaterial;equipped.enabled=body.enabled;
            }
            else equipped.enabled=false;
            return;
        }
        Sprite replacement;
        if(!clean.TryGetValue(original,out replacement)){equipped.enabled=false;return;}
        body.sprite=replacement;
        WeaponFrame authored;
        if(animation.TryGetValue(original,out authored))
        {
            CurrentBinding = authored;
            float direction=body.flipX?-1:1;
            equipped.sprite=authored.sprite;
            equipped.transform.localPosition=new Vector3(authored.hand.x*direction,authored.hand.y,0);
            equipped.transform.localRotation=Quaternion.identity;
            equipped.transform.localScale=Vector3.one;
            equipped.flipX=body.flipX ^ authored.flipHorizontal;
            equipped.flipY=authored.flipVertical;
            equipped.sortingLayerID=body.sortingLayerID;
            equipped.sortingOrder=body.sortingOrder+authored.sortingOffset;
            equipped.color=body.color;equipped.sharedMaterial=body.sharedMaterial;
            equipped.enabled=body.enabled&&!authored.hidden&&!controller.isDead&&!controller.IsClimbingLadder&&authored.sprite!=null;
            return;
        }
        bool extra=original.name.StartsWith("RedKnightSkill_");
        int index; if(!int.TryParse(original.name.Substring(extra?15:10),out index)){equipped.enabled=false;return;}
        bool hidden=controller.isDead||controller.IsClimbingLadder||(!extra&&index>=42&&index<=53);
        Vector2 grip=new Vector2(.32f,.29f);float angle=-110;
        if(!extra&&index>=6&&index<=13)
        {
            grip=AttackGrips[index-6];angle=AttackAngles[index-6];
        }
        else if(!extra&&index>=14&&index<=21){grip=new Vector2(index%8<4?.56f:.40f,.44f);angle=20;}
        else if(!extra&&index>=54&&index<=61){grip=new Vector2(.33f,.63f);angle=10;}
        else if(extra)
        {
            if(index>=30)hidden=true;
            else if(index>=22){grip=new Vector2(index==22?.24f:index==23?.32f:.66f,index<=23?.66f:.34f);angle=index==22?70:index==23?80:-90;}
            else if(index>=18){grip=new Vector2(.64f,.55f);angle=15;}
            else if(index>=10){grip=new Vector2(.38f,.22f);angle=-85;}
            else{grip=new Vector2(.38f,.32f);angle=index>=4?-90:-110;}
        }
        Vector2 hand=new Vector2((grip.x*original.rect.width-original.pivot.x)/original.pixelsPerUnit,
            (grip.y*original.rect.height-original.pivot.y)/original.pixelsPerUnit);
        float facing=body.flipX?-1:1;
        equipped.transform.localPosition=new Vector3(hand.x*facing,hand.y,0);
        equipped.transform.localRotation=Quaternion.Euler(0,0,angle*facing);
        equipped.transform.localScale=new Vector3(facing,1,1);
        equipped.flipX=false;equipped.flipY=false;
        equipped.sortingLayerID=body.sortingLayerID;equipped.sortingOrder=body.sortingOrder+1;
        equipped.color=body.color;equipped.sharedMaterial=body.sharedMaterial;
        equipped.enabled=body.enabled&&!hidden;
    }
    private void OnDisable()
    {
        if(equipped!=null)equipped.enabled=false;
        Sprite original;if(body!=null&&body.sprite!=null&&originals.TryGetValue(body.sprite,out original))body.sprite=original;
    }
    private void OnDestroy()
    {
        foreach(var sprite in clean.Values)if(sprite!=null)Destroy(sprite);
    }
}
