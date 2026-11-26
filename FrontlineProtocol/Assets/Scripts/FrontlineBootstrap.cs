using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// FRONTLINE PROTOCOL - procedural Unity FPS foundation.
public class FrontlineBootstrap : MonoBehaviour
{
    public static FrontlineBootstrap Instance;
    public bool Playing { get; private set; }
    public Transform Player { get; private set; }
    public Camera ViewCamera { get; private set; }
    public int Kills { get; private set; }
    public int Wave = 1, Health = 100;
    readonly List<EnemyBot> enemies = new List<EnemyBot>();
    Material concrete, steel, blue, red, olive, dark, glass, amber;
    GUIStyle title, label, small;
    WeaponSystem weapons;
    bool paused, settings;
    float sensitivity=2, fov=88, damageFlash, hitFlash, spawnTimer;
    int quality=2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        if (FindObjectOfType<FrontlineBootstrap>() == null)
            new GameObject("FRONTLINE_PROTOCOL").AddComponent<FrontlineBootstrap>();
    }
    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance=this; Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
        QualitySettings.shadowDistance=110; QualitySettings.antiAliasing=4;
        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        MakeMaterials(); BuildDockyard(); BuildPlayer(); BuildStyles(); SpawnWave(5);
    }
    Material Mat(string n, Color c, float metal=0, float gloss=.35f) {
        var m=new Material(Shader.Find("Standard")) { name=n, color=c };
        m.SetFloat("_Metallic",metal); m.SetFloat("_Glossiness",gloss); return m;
    }
    void MakeMaterials() {
        concrete=Mat("Dock asphalt",new Color(.25f,.29f,.31f),.08f,.3f);
        steel=Mat("Brushed steel",new Color(.24f,.29f,.32f),.82f,.25f);
        blue=Mat("Ocean blue container",new Color(.035f,.19f,.29f),.35f,.55f);
        red=Mat("Oxide red container",new Color(.39f,.075f,.045f),.28f,.62f);
        olive=Mat("Military olive",new Color(.16f,.21f,.13f),.25f,.7f);
        dark=Mat("Rubber black",new Color(.025f,.035f,.04f),.25f,.8f);
        glass=Mat("Armored glass",new Color(.16f,.32f,.39f,.4f),.7f,.1f);
        amber=Mat("Safety amber",new Color(1,.28f,.025f),.3f,.2f);
        Detail(blue,new Color(.035f,.19f,.29f)); Detail(red,new Color(.39f,.075f,.045f));
        Detail(concrete,new Color(.25f,.29f,.31f));
    }
    void Detail(Material m, Color c) {
        const int n=128; var t=new Texture2D(n,n,TextureFormat.RGBA32,true);
        t.wrapMode=TextureWrapMode.Repeat; var rng=new System.Random(m.name.GetHashCode());
        for(int y=0;y<n;y++) for(int x=0;x<n;x++) {
            float v=Mathf.Clamp01(1+(float)rng.NextDouble()*.11f-.055f-((x%16==0||y%32==0)?.09f:0));
            t.SetPixel(x,y,new Color(c.r*v,c.g*v,c.b*v,1));
        }
        t.Apply(true); m.mainTexture=t;
    }
    GameObject Cube(string n,Vector3 p,Vector3 s,Material m) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=n;
        g.transform.position=p; g.transform.localScale=s; g.GetComponent<Renderer>().material=m; return g;
    }
    void Container(Vector3 p,Material m) {
        Cube("Ribbed shipping container",p,new Vector3(11,4.4f,8),m);
        for(int i=0;i<18;i++) Cube("Corrugation",p+new Vector3(-5.3f+i*.62f,0,-4.06f),new Vector3(.055f,4.2f,.08f),steel);
        Cube("Container doors",p+new Vector3(0,0,4.08f),new Vector3(10.6f,4.1f,.12f),dark);
        for(int i=0;i<4;i++) Cube("Door lock",p+new Vector3(-4+i*2.65f,0,4.18f),new Vector3(.12f,3.8f,.1f),steel);
    }
    void BuildDockyard() {
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.42f,.52f,.63f);
        RenderSettings.ambientEquatorColor=new Color(.28f,.31f,.34f);
        RenderSettings.ambientGroundColor=new Color(.12f,.13f,.14f);
        RenderSettings.fog=true; RenderSettings.fogColor=new Color(.48f,.55f,.59f);
        RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.0035f;
        Cube("Asphalt combat yard",new Vector3(0,-.5f,0),new Vector3(100,1,100),concrete).isStatic=true;
        var sun=new GameObject("Midday sun").AddComponent<Light>();
        sun.type=LightType.Directional; sun.intensity=1.35f; sun.transform.rotation=Quaternion.Euler(48,-32,0); sun.shadows=LightShadows.Soft;
        for(int i=0;i<5;i++) { float z=-22+i*11; Container(new Vector3(-19,2.2f,z),blue); Container(new Vector3(19,2.2f,z+4),red); }
        Container(new Vector3(-2,2.2f,-27),olive); Container(new Vector3(3,2.2f,28),blue);
        Container(new Vector3(-19,6.7f,0),red); Container(new Vector3(19,6.7f,-7),olive);
        Cube("Warehouse",new Vector3(0,5,43),new Vector3(38,10,12),steel);
        Cube("Loading bay",new Vector3(0,2.8f,36.8f),new Vector3(12,5.6f,.3f),dark);
        Cube("Crane cross beam",new Vector3(0,16,8),new Vector3(58,1.2f,1.4f),amber);
        Cube("Crane tower L",new Vector3(-27,8,8),new Vector3(1.5f,16,1.5f),steel);
        Cube("Crane tower R",new Vector3(27,8,8),new Vector3(1.5f,16,1.5f),steel);
        Cube("Crane trolley",new Vector3(0,14.8f,8),new Vector3(5,1,3),dark);
        Cube("Crane cable",new Vector3(0,10,8),new Vector3(.12f,9,.12f),dark);
        Cube("Crane spreader",new Vector3(0,5.3f,8),new Vector3(7,.5f,2),amber);
        for(int i=0;i<18;i++) {
            float x=Mathf.Sin(i*2.4f)*31, z=Mathf.Cos(i*1.7f)*32;
            if(Mathf.Abs(x)<13&&Mathf.Abs(z)<13) continue;
            Cube("Cargo pallet",new Vector3(x,.65f,z),new Vector3(2.5f,1.3f,2),i%2==0?steel:olive);
        }
        for(int i=0;i<8;i++) Cube("Concrete barrier",new Vector3(-8+i*2.3f,.65f,12),new Vector3(1.8f,1.3f,.9f),concrete);
        for(int i=0;i<5;i++) {
            var b=GameObject.CreatePrimitive(PrimitiveType.Cylinder); b.name="Explosive barrel";
            b.transform.position=new Vector3(-10+i*5,1,-8+(i%2)*4); b.transform.localScale=new Vector3(.7f,1,.7f);
            b.GetComponent<Renderer>().material=amber; b.AddComponent<ExplosiveBarrel>();
        }
        for(int i=0;i<4;i++) {
            var pane=Cube("Breakable glass",new Vector3(-7+i*4,4.3f,36.5f),new Vector3(3.7f,2.5f,.16f),glass);
            pane.AddComponent<BreakableGlass>();
        }
        for(int i=0;i<6;i++) {
            var lgo=new GameObject("Dock floodlight"); lgo.transform.position=new Vector3(-25+i*10,10,24);
            var l=lgo.AddComponent<Light>(); l.type=LightType.Point; l.range=20; l.intensity=3; l.color=new Color(1,.55f,.28f);
        }
        Cube("North perimeter",new Vector3(0,3,51),new Vector3(105,6,1),dark);
        Cube("South perimeter",new Vector3(0,3,-51),new Vector3(105,6,1),dark);
        Cube("East perimeter",new Vector3(51,3,0),new Vector3(1,6,105),dark);
        Cube("West perimeter",new Vector3(-51,3,0),new Vector3(1,6,105),dark);
    }
    void BuildPlayer() {
        var p=new GameObject("Player Operator"); p.transform.position=new Vector3(0,1.2f,-39);
        var cc=p.AddComponent<CharacterController>(); cc.height=1.8f; cc.radius=.36f; cc.center=new Vector3(0,.9f,0);
        Player=p.transform; var c=new GameObject("FPS Camera"); c.transform.SetParent(Player);
        c.transform.localPosition=new Vector3(0,1.58f,0); ViewCamera=c.AddComponent<Camera>();
        ViewCamera.fieldOfView=fov; ViewCamera.nearClipPlane=.04f; ViewCamera.farClipPlane=180;
        c.AddComponent<AudioListener>(); p.AddComponent<FPSMotor>(); weapons=p.AddComponent<WeaponSystem>(); weapons.Init(ViewCamera);
    }
    void BuildStyles() {
        title=new GUIStyle(GUI.skin.label){fontSize=44,fontStyle=FontStyle.Bold};
        title.normal.textColor=Color.white;
        label=new GUIStyle(GUI.skin.label){fontSize=17,fontStyle=FontStyle.Bold}; label.normal.textColor=Color.white;
        small=new GUIStyle(GUI.skin.label){fontSize=12}; small.normal.textColor=new Color(.7f,.8f,.85f);
    }
    void SpawnWave(int count) {
        for(int i=0;i<count;i++) {
            Vector3 p=new Vector3(Random.Range(-34,34),0,Random.Range(-26,35));
            if(Vector3.Distance(p,Player.position)<16) p+=new Vector3(24,0,18);
            var bot=new GameObject("Hostile Raider"); bot.transform.position=p; enemies.Add(bot.AddComponent<EnemyBot>());
        }
    }
    public void StartMatch() { Playing=true; paused=false; Health=100; Kills=0; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; Player.position=new Vector3(0,1.2f,-39); }
    void Pause(bool v) { paused=v; Cursor.lockState=v?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=v; }
    public void EnemyKilled(EnemyBot e) { Kills++; enemies.Remove(e); weapons.AddReserve(6); }
    public void PlayerHit(int d) { Health=Mathf.Max(0,Health-d); damageFlash=1; }
    public void Hit() { hitFlash=1; }
    void Update() {
        if(Playing&&!paused) {
            if(Input.GetKeyDown(KeyCode.Escape)) Pause(true);
            if(Health<=0) { Playing=false; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
            spawnTimer+=Time.deltaTime;
            if(enemies.Count==0&&spawnTimer>3) { Wave++; SpawnWave(4+Wave); spawnTimer=0; }
        }
        damageFlash=Mathf.MoveTowards(damageFlash,0,Time.deltaTime*.75f);
        hitFlash=Mathf.MoveTowards(hitFlash,0,Time.deltaTime*5);
    }
    void OnGUI() {
        if(title==null) BuildStyles();
        if(!Playing&&Health>0) {
            Backdrop(); GUILayout.BeginArea(new Rect(Screen.width*.08f,Screen.height*.18f,Screen.width*.7f,Screen.height*.7f));
            GUILayout.Label("FRONTLINE",title);
            var orange=new GUIStyle(title){fontSize=60}; orange.normal.textColor=new Color(1,.36f,.08f);
            GUILayout.Label("PROTOCOL",orange); GUILayout.Space(12);
            GUILayout.Label("OPERATION IRON TIDE / CARGO DOCKYARD",label); GUILayout.Space(24);
            if(GUILayout.Button("DEPLOY — START MATCH",GUILayout.Width(290),GUILayout.Height(48))) StartMatch();
            GUILayout.Space(12); GUILayout.Label("WASD MOVE • SHIFT SPRINT • CTRL CROUCH • SPACE JUMP",small);
            GUILayout.Label("MOUSE AIM • LMB FIRE • RMB ADS • R RELOAD • 1 / 2 / 3 WEAPONS",small);
            GUILayout.EndArea(); return;
        }
        if(Playing&&!paused) DrawHUD();
        if(paused||Health<=0) {
            Backdrop(); GUILayout.BeginArea(new Rect(Screen.width*.34f,Screen.height*.2f,Screen.width*.34f,Screen.height*.65f));
            GUILayout.Label(Health<=0?"OPERATOR DOWN":"PAUSED",title);
            if(Health<=0) { GUILayout.Label("ELIMINATIONS: "+Kills,label); if(GUILayout.Button("REDEPLOY",GUILayout.Height(42))) StartMatch(); }
            else {
                if(GUILayout.Button("RESUME",GUILayout.Height(42))) Pause(false);
                if(GUILayout.Button("SETTINGS",GUILayout.Height(36))) settings=!settings;
                if(settings) {
                    GUILayout.Label("MOUSE SENSITIVITY "+sensitivity.ToString("0.0")); sensitivity=GUILayout.HorizontalSlider(sensitivity,.5f,5);
                    GUILayout.Label("FIELD OF VIEW "+fov.ToString("0")); fov=GUILayout.HorizontalSlider(fov,65,110); ViewCamera.fieldOfView=fov;
                    GUILayout.Label("QUALITY "+quality); quality=(int)GUILayout.HorizontalSlider(quality,0,3); QualitySettings.SetQualityLevel(quality);
                }
            }
            GUILayout.EndArea();
        }
    }
    void Backdrop() {
        GUI.color=new Color(.015f,.025f,.04f,.96f); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
        GUI.color=new Color(1,.28f,.05f,.6f); GUI.DrawTexture(new Rect(0,Screen.height*.78f,Screen.width,3),Texture2D.whiteTexture); GUI.color=Color.white;
    }
    void DrawHUD() {
        float x=Screen.width*.5f,y=Screen.height*.5f; GUI.color=Color.white;
        GUI.DrawTexture(new Rect(x-1,y-9,2,6),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(x-1,y+3,2,6),Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x-9,y-1,6,2),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(x+3,y-1,6,2),Texture2D.whiteTexture);
        if(hitFlash>0) { GUI.color=new Color(1,.65f,.15f,hitFlash); GUI.DrawTexture(new Rect(x-11,y-1,22,2),Texture2D.whiteTexture); GUI.DrawTexture(new Rect(x-1,y-11,2,22),Texture2D.whiteTexture); }
        GUI.color=Color.white; GUI.Label(new Rect(24,20,500,26),"OPERATION IRON TIDE",label);
        GUI.Label(new Rect(24,48,400,22),"WAVE "+Wave.ToString("00")+"   ELIMINATIONS "+Kills.ToString("00"),small);
        GUI.Label(new Rect(24,Screen.height-74,200,28),"HEALTH  "+Health,label);
        GUI.color=new Color(.8f,.12f,.08f,.9f); GUI.DrawTexture(new Rect(24,Screen.height-43,220,9),Texture2D.whiteTexture);
        GUI.color=new Color(.15f,.85f,.35f,.95f); GUI.DrawTexture(new Rect(24,Screen.height-43,220*Health/100f,9),Texture2D.whiteTexture);
        GUI.color=Color.white; weapons.DrawAmmo(Screen.width,Screen.height);
        Rect r=new Rect(Screen.width-174,20,150,150); GUI.color=new Color(.015f,.07f,.08f,.8f); GUI.DrawTexture(r,Texture2D.whiteTexture);
        GUI.color=new Color(.1f,.8f,.75f,.9f); GUI.DrawTexture(new Rect(r.center.x,r.center.y,2,2),Texture2D.whiteTexture);
        foreach(var e in enemies) if(e!=null) { Vector3 d=e.transform.position-Player.position; GUI.color=Color.red; GUI.DrawTexture(new Rect(r.center.x+Mathf.Clamp(d.x,-45,45)*1.5f-2,r.center.y-Mathf.Clamp(d.z,-45,45)*1.5f-2,5,5),Texture2D.whiteTexture); }
        if(damageFlash>0) { GUI.color=new Color(.6f,0,0,damageFlash*.32f); GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); }
        GUI.color=Color.white;
    }
}

public class FPSMotor : MonoBehaviour {
    CharacterController cc; Transform cam; float pitch,vy,bob,tilt,height=1.8f;
    void Start(){cc=GetComponent<CharacterController>();cam=FrontlineBootstrap.Instance.ViewCamera.transform;}
    void Update(){
        var g=FrontlineBootstrap.Instance;if(!g.Playing)return;
        float sens=2f; transform.Rotate(Vector3.up*Input.GetAxis("Mouse X")*sens*2.2f);
        pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*sens*2.2f,-84,84);
        bool crouch=Input.GetKey(KeyCode.LeftControl), sprint=Input.GetKey(KeyCode.LeftShift)&&!crouch;
        height=Mathf.Lerp(height,crouch?1.12f:1.8f,Time.deltaTime*12);cc.height=height;cc.center=new Vector3(0,height*.5f,0);
        Vector3 input=Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical")),1);
        Vector3 move=transform.TransformDirection(input)*(crouch?3.2f:sprint?9.2f:5.7f);
        if(cc.isGrounded){vy=-2;if(Input.GetKeyDown(KeyCode.Space)&&!crouch)vy=7.2f;}else vy-=20*Time.deltaTime;
        move.y=vy;cc.Move(move*Time.deltaTime);
        bool moving=input.sqrMagnitude>.05f&&cc.isGrounded;bob=Mathf.Lerp(bob,moving?(sprint?1.4f:.8f):0,Time.deltaTime*8);
        float wave=moving?Mathf.Sin(Time.time*(sprint?14:10))*.045f*bob:0;
        tilt=Mathf.Lerp(tilt,-input.x*(sprint?7:4),Time.deltaTime*7);
        cam.localPosition=new Vector3(0,height-.22f+wave,0);cam.localRotation=Quaternion.Euler(pitch,0,tilt);
        if(Input.GetKeyDown(KeyCode.Alpha1))g.GetComponent<WeaponSystem>().Equip(0);
        if(Input.GetKeyDown(KeyCode.Alpha2))g.GetComponent<WeaponSystem>().Equip(1);
        if(Input.GetKeyDown(KeyCode.Alpha3))g.GetComponent<WeaponSystem>().Equip(2);
    }
}

public class WeaponSystem : MonoBehaviour {
    class Gun { public string name; public int mag,reserve,capacity,damage; public float rate,reload,spread; }
    Gun[] guns={
        new Gun{name="VX-7 ASSAULT",mag=30,reserve=150,capacity=30,damage=24,rate=.095f,reload=1.8f,spread=.018f},
        new Gun{name="M90 PRECISION",mag=5,reserve=35,capacity=5,damage=100,rate=.85f,reload=2.5f,spread=.001f},
        new Gun{name="TAC-9 SIDEARM",mag=15,reserve=90,capacity=15,damage=34,rate=.22f,reload=1.35f,spread=.025f}};
    int current; Camera cam; Transform root; GameObject flash; float next,recoil,reloadT; bool reloading;
    AudioSource audio; AudioClip shot,reload; LineRenderer tracer;
    public void Init(Camera c){cam=c;audio=gameObject.AddComponent<AudioSource>();shot=Tone(100,.12f);reload=Tone(420,.07f);Build();}
    AudioClip Tone(float hz,float duration){int rate=22050,n=(int)(rate*duration);float[] a=new float[n];for(int i=0;i<n;i++){float t=(float)i/rate;a[i]=Mathf.Sin(2*Mathf.PI*hz*t)*Mathf.Exp(-t*28)*.35f;}var c=AudioClip.Create("Synthesized SFX",n,1,rate,false);c.SetData(a,0);return c;}
    void Build(){
        if(root!=null)Destroy(root.gameObject);root=new GameObject("First Person Weapon").transform;root.SetParent(cam.transform);root.localPosition=new Vector3(.32f,-.28f,.62f);
        Material b=new Material(Shader.Find("Standard"));b.color=new Color(.035f,.045f,.05f);b.SetFloat("_Metallic",.8f);
        Material m=new Material(Shader.Find("Standard"));m.color=new Color(.25f,.3f,.33f);m.SetFloat("_Metallic",.95f);
        Part("Receiver",new Vector3(0,0,0),new Vector3(.18f,.16f,.58f),b);Part("Barrel",new Vector3(0,.015f,.43f),new Vector3(.065f,.065f,.48f),m);
        Part("Handguard",new Vector3(0,-.01f,.29f),new Vector3(.14f,.12f,.34f),b);Part("Grip",new Vector3(0,-.17f,-.08f),new Vector3(.12f,.25f,.13f),b);
        Part("Stock",new Vector3(0,-.01f,-.43f),new Vector3(.15f,.13f,.28f),b);Part("Holo optic",new Vector3(0,.13f,.04f),new Vector3(.12f,.1f,.13f),m);
        flash=GameObject.CreatePrimitive(PrimitiveType.Sphere);flash.transform.SetParent(root);flash.transform.localPosition=new Vector3(0,.01f,.7f);flash.transform.localScale=Vector3.one*.12f;flash.GetComponent<Renderer>().material.color=new Color(1,.42f,.05f);Destroy(flash.GetComponent<Collider>());flash.SetActive(false);
        tracer=gameObject.AddComponent<LineRenderer>();tracer.enabled=false;tracer.positionCount=2;tracer.startWidth=.025f;tracer.endWidth=.002f;tracer.material=new Material(Shader.Find("Sprites/Default"));tracer.material.color=new Color(1,.65f,.18f);
    }
    void Part(string n,Vector3 p,Vector3 s,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(root);o.transform.localPosition=p;o.transform.localScale=s;o.GetComponent<Renderer>().material=m;Destroy(o.GetComponent<Collider>());}
    void Update(){
        var g=FrontlineBootstrap.Instance;if(!g.Playing)return;
        if(Input.GetKeyDown(KeyCode.R))Reload();
        if(Input.GetMouseButton(0)&&Time.time>=next&&!reloading)Fire();
        if(reloading){reloadT-=Time.deltaTime;if(reloadT<=0){var w=guns[current];int n=Mathf.Min(w.capacity-w.mag,w.reserve);w.mag+=n;w.reserve-=n;reloading=false;}}
        recoil=Mathf.Lerp(recoil,0,Time.deltaTime*8);float aim=Input.GetMouseButton(1)?1:0;
        root.localPosition=Vector3.Lerp(root.localPosition,new Vector3(.32f,-.28f,.62f)-new Vector3(.12f,.08f,.22f)*aim,Time.deltaTime*10);
        root.localRotation=Quaternion.Slerp(root.localRotation,Quaternion.Euler(-recoil*2,0,0),Time.deltaTime*12);
    }
    void Fire(){
        Gun w=guns[current];if(w.mag<=0){Reload();return;}w.mag--;next=Time.time+w.rate;recoil+=current==1?5.5f:1.6f;flash.SetActive(true);audio.PlayOneShot(shot);
        Vector3 d=cam.transform.forward+cam.transform.right*Random.Range(-w.spread,w.spread)+cam.transform.up*Random.Range(-w.spread,w.spread);
        Ray ray=new Ray(cam.transform.position,d.normalized);Vector3 end=ray.origin+ray.direction*120;
        if(Physics.Raycast(ray,out RaycastHit hit,120)){
            end=hit.point;EnemyBot e=hit.collider.GetComponentInParent<EnemyBot>();
            if(e!=null){bool head=hit.collider.name=="Head hitbox";e.TakeDamage(w.damage*(head?2:1));FrontlineBootstrap.Instance.Hit();}
            var barrel=hit.collider.GetComponent<ExplosiveBarrel>();if(barrel!=null)barrel.Detonate();
            var glass=hit.collider.GetComponent<BreakableGlass>();if(glass!=null)glass.Break();
        }
        StartCoroutine(Trace(cam.transform.position+cam.transform.forward*.5f,end));
        var casing=GameObject.CreatePrimitive(PrimitiveType.Capsule);casing.transform.position=root.position+root.right*.18f;casing.transform.localScale=new Vector3(.025f,.05f,.025f);
        var rb=casing.AddComponent<Rigidbody>();rb.AddForce(cam.transform.right*1.5f+cam.transform.up,ForceMode.Impulse);Destroy(casing,2.5f);
        StartCoroutine(FlashOff());
    }
    IEnumerator FlashOff(){yield return new WaitForSeconds(.045f);flash.SetActive(false);}
    IEnumerator Trace(Vector3 a,Vector3 b){tracer.enabled=true;tracer.SetPosition(0,a);tracer.SetPosition(1,b);yield return new WaitForSeconds(.045f);tracer.enabled=false;}
    void Reload(){Gun w=guns[current];if(reloading||w.mag==w.capacity||w.reserve<=0)return;reloading=true;reloadT=w.reload;audio.PlayOneShot(reload);}
    public void Equip(int i){if(i<0||i>2||i==current)return;current=i;Build();}
    public void AddReserve(int n){guns[current].reserve+=n;}
    public void DrawAmmo(float sw,float sh){Gun w=guns[current];var s=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleRight};s.normal.textColor=Color.white;GUI.Label(new Rect(sw-245,sh-82,215,34),w.mag+" / "+w.reserve,s);GUI.Label(new Rect(sw-245,sh-49,215,20),w.name,new GUIStyle(s){fontSize=12});}
}

public class EnemyBot : MonoBehaviour {
    Transform player;float hp=100,shotTimer,moveTimer;Vector3 patrol;Renderer body;
    void Start(){
        player=FrontlineBootstrap.Instance.Player;Material armor=new Material(Shader.Find("Standard"));armor.color=new Color(.12f,.16f,.18f);armor.SetFloat("_Metallic",.55f);
        var torso=GameObject.CreatePrimitive(PrimitiveType.Capsule);torso.name="Armored torso";torso.transform.SetParent(transform);torso.transform.localPosition=new Vector3(0,.95f,0);torso.transform.localScale=new Vector3(.72f,.9f,.55f);body=torso.GetComponent<Renderer>();body.material=armor;
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Head hitbox";head.transform.SetParent(transform);head.transform.localPosition=new Vector3(0,1.8f,0);head.transform.localScale=Vector3.one*.42f;head.GetComponent<Renderer>().material=armor;
        var visor=GameObject.CreatePrimitive(PrimitiveType.Cube);visor.transform.SetParent(head.transform);visor.transform.localPosition=new Vector3(0,0,.9f);visor.transform.localScale=new Vector3(.8f,.18f,.12f);visor.GetComponent<Renderer>().material.color=new Color(1,.08f,.035f);Destroy(visor.GetComponent<Collider>());
        patrol=transform.position+new Vector3(Random.Range(-8,8),0,Random.Range(-8,8));
    }
    void Update(){
        if(player==null||!FrontlineBootstrap.Instance.Playing)return;Vector3 d=player.position-transform.position;float dist=d.magnitude;
        if(dist<34){d.y=0;if(dist>9)transform.position+=d.normalized*2.7f*Time.deltaTime;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),Time.deltaTime*5);
            shotTimer-=Time.deltaTime;if(dist<30&&shotTimer<=0){shotTimer=Random.Range(.65f,1.35f);if(Random.value<.48f)FrontlineBootstrap.Instance.PlayerHit(Random.Range(5,10));}}
        else{moveTimer-=Time.deltaTime;if(moveTimer<=0){patrol=transform.position+new Vector3(Random.Range(-12,12),0,Random.Range(-12,12));moveTimer=Random.Range(2,5);}Vector3 v=patrol-transform.position;v.y=0;if(v.magnitude>1)transform.position+=v.normalized*1.5f*Time.deltaTime;}
        transform.position=new Vector3(transform.position.x,0,transform.position.z);
    }
    public void TakeDamage(int n){hp-=n;if(body!=null)body.material.color=Color.white;if(hp<=0){FrontlineBootstrap.Instance.EnemyKilled(this);transform.Rotate(0,0,82);transform.position+=Vector3.down*.35f;Destroy(gameObject,1.2f);}}
}
public class ExplosiveBarrel : MonoBehaviour {
    bool spent;public void Detonate(){if(spent)return;spent=true;foreach(var c in Physics.OverlapSphere(transform.position,8)){var e=c.GetComponentInParent<EnemyBot>();if(e!=null)e.TakeDamage(120);}Destroy(gameObject);}
}
public class BreakableGlass : MonoBehaviour {
    bool broken;public void Break(){if(broken)return;broken=true;for(int i=0;i<8;i++){var s=GameObject.CreatePrimitive(PrimitiveType.Cube);s.transform.position=transform.position+Random.insideUnitSphere*.6f;s.transform.localScale=new Vector3(.12f,.18f,.035f);s.GetComponent<Renderer>().material.color=new Color(.25f,.55f,.65f,.5f);s.AddComponent<Rigidbody>().AddForce(Random.insideUnitSphere*3,ForceMode.Impulse);Destroy(s,2);}Destroy(gameObject);}
}
