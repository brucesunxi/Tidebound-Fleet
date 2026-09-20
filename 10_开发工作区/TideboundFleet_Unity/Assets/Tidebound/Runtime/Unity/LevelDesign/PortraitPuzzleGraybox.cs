using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Combat;
using Tidebound.Tools;
using Tidebound.Save;
using Tidebound.Unity.Boss;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using Tidebound.Unity.Input;
using Tidebound.Unity.Lane;
using Tidebound.Unity.Layout;
using Tidebound.Unity.Ship;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>Standalone 5R playable candidate scene. All actions use the production model/controllers.</summary>
    public sealed partial class PortraitPuzzleGraybox : MonoBehaviour
    {
        [SerializeField] private TextAsset manifest;
        [SerializeField] private TextAsset[] layouts;
        [SerializeField] private TextAsset[] proofs;
        private CandidateLevelCatalog catalog;
        private SavedGameRuntime world;
        private PlayerSaveService saveService;
        private bool campaign;
        private float victoryWait;
        private Text wallet;
        public PlayerSaveService SaveService => saveService;
        private GameSession session;
        private ShipMovementController movement;
        private TransitSystem transit;
        private ShipToolSystem tools;
        private ToolInventory toolInventory;
        private readonly ToolGiftPolicy toolGiftPolicy=new ToolGiftPolicy();
        private BoardProgressMonitor progress;
        private RectTransform menuPanel, acquisitionPanel;
        private bool acquisitionPauseOwned;
        private CoinShopPanel shopPanel;
        public CoinShopPanel ShopPanel => shopPanel;
        private bool menuPauseOwned;
        private Button rescueButton,shuffleButton,reverseButton;
        private FleetCombatSystem combat;
        private FleetCombatGrayboxView combatView;
        private RectTransform battlePanel;
        private LaneTransitController lane;
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();
        private readonly Dictionary<string, ShipMovementView> shipViews = new Dictionary<string, ShipMovementView>();
        private readonly List<string> exited = new List<string>();
        private readonly List<string> entered = new List<string>();
        private GameObject presentation;
        private Camera boardCamera;
        private GridWorldMapper mapper;
        private BoardGridInputRouter input;
        private Canvas overlay;
        private RectTransform topPanel, toolsPanel, lanePanel;
        private Text title, status;
        private Button pause, auto, hint;
        private Queue<string> demo;
        private Font font;
        private Rect lastSafe;
        private Vector2Int lastScreen;
        private string notice = "Tap a ship to move forward.";
        private ShipMovementTiming movementTiming;
        private LaneTransitTiming transitTiming;
        private CombatTiming combatTiming;
        private float elapsedSinceDemo;
        private bool initialized;

        public GameSession Session => session;
        public Camera BoardCamera => boardCamera;
        public BoardGridInputRouter InputSurface => input;
        public PortraitBoardLayout Layout { get; private set; }
        public int LevelIndex { get; private set; }
        public bool IsBusy => movement?.ActiveOperation != null;
        public bool IsPaused => session?.State == GameState.Paused;
        public bool IsAutoPlaying => demo != null;
        public int ActiveViewCount => shipViews.Values.Count(v => v != null && v.gameObject.activeSelf);
        public IReadOnlyList<string> ExitedIds => exited;
        public IReadOnlyList<string> EnteredIds => entered;
        public bool IsCleared => combat != null && combat.IsVictorious;
        public FleetCombatSystem Combat => combat;
        public ShipToolSystem Tools => tools;
        public ToolInventory ToolInventory => toolInventory;
        public BoardProgressMonitor Progress => progress;
        public bool IsMenuOpen => menuPanel!=null && menuPanel.gameObject.activeSelf;
        public bool IsAcquisitionOpen => acquisitionPanel!=null && acquisitionPanel.gameObject.activeSelf;
        public FleetCombatGrayboxView CombatView => combatView;
        public Vector3 ViewPosition(string id) => shipViews[id].transform.position;

        private void Start()
        {
            if (initialized || manifest == null) return;
            autoHintsEnabled = PlayerPrefs.GetInt("Tidebound.AutoHints", 1) != 0;
            reducedHintMotion = PlayerPrefs.GetInt("Tidebound.ReducedHintMotion", 0) != 0;
            reducedEntryMotion = PlayerPrefs.GetInt("Tidebound.ReducedEntryMotion", 0) != 0;
            try
            {
                var saveRoot=Application.persistentDataPath;
#if UNITY_EDITOR
                // Keep editor trials project-local, separate from installed player data.
                saveRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/Tidebound"));
#endif
                Initialize(new CandidateLevelCatalog(manifest.text,layouts.Select(x=>x.text),proofs.Select(x=>x.text)),
                    saveService:new PlayerSaveService(new PlayerSaveFileStore(Path.Combine(saveRoot,"player-save-v2.json")),
                        new ToolInventoryFileStore(Path.Combine(saveRoot,"tool-inventory-v1.json"))),campaign:true, animateEntry:true);
            }
            catch (Exception e) { Debug.LogError("Graybox candidate validation failed: " + e.Message); enabled = false; }
        }

        public void ConfigureAssets(TextAsset manifestAsset, TextAsset[] levelAssets, TextAsset[] proofAssets)
        { manifest = manifestAsset; layouts = levelAssets; proofs = proofAssets; }

        public void Initialize(CandidateLevelCatalog source, ShipMovementTiming timing = null, LaneTransitTiming laneTiming = null, CombatTiming combatTiming = null, ToolInventory inventory = null,
            PlayerSaveService saveService = null, bool campaign = false, bool animateEntry = false)
        {
            FlushSave();ClearSession();
            catalog = source ?? throw new ArgumentNullException(nameof(source));
            this.saveService=saveService;this.campaign=campaign;this.animateEntry=animateEntry;
            toolInventory=saveService?.Inventory ?? inventory ?? new ToolInventory();
            movementTiming = timing ?? new ShipMovementTiming();
            transitTiming = laneTiming ?? new LaneTransitTiming();
            this.combatTiming = combatTiming ?? new CombatTiming();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            initialized = true;
            var saved=saveService?.Snapshot.Attempt;
            if(saveService?.IsAvailable==true && saved!=null)
            {
                try
                {
                    // Validate against the shipped catalog before attaching a persisted run.
                    var index=saved.LevelNumber-1;
                    using(var reference=SavedGameRuntime.Create(catalog.Load(index),saved.LevelNumber))
                        if(reference.Capture().LayoutFingerprint!=saved.LayoutFingerprint)
                            throw new InvalidOperationException("Saved level differs from the installed catalog.");
                    var restored=SavedGameRuntime.Restore(saved);saveService.AttachRestored(restored);
                    BindWorld(restored,index);saveService.Inventory.ReachLevel(saveService.CurrentLevel);
                    SaveCheckpoint(true);BeginEntry(true);
                }
                catch(Exception e){saveService.Suspend(e.GetType().Name);ClearSession();SelectLevel(0);}
            }
            else SelectLevel(saveService?.IsAvailable==true ? saveService.CurrentLevel-1 : 0);
        }

        // Editor inspection is deliberately isolated from campaign progress and currency.
        public void EnableReviewMode(PlayerSaveService reviewSave=null, bool animateEntry=false) => Initialize(catalog,saveService:reviewSave,animateEntry:animateEntry);

        public void SelectLevel(int index)
        {
            if (entryRequestInFlight || !IsEntryReady || IsEntrySaveBlocked) return;
            if (catalog == null) throw new InvalidOperationException("Load a validated catalog first.");
            if (index < 0 || index >= catalog.Count) throw new ArgumentOutOfRangeException(nameof(index));
            if(saveService?.IsAvailable==true)
            {
                if(world!=null && IsCleared && !saveService.CurrentAttemptSettled)return;
                if(index!=saveService.CurrentLevel-1 || (world!=null && !IsCleared))return;
                CommitEntry(SavedGameRuntime.Create(catalog.Load(index),index+1,transitTiming,combatTiming),index,false);return;
            }
            progress?.EndForRestart();ClearSession();
            BindWorld(SavedGameRuntime.Create(catalog.Load(index),index+1,transitTiming,combatTiming),index);BeginEntry(false);
        }

        private void BindWorld(SavedGameRuntime runtime,int index)
        {
            world=runtime;LevelIndex=index;victoryWait=0;session=world.Session;
            presentation = new GameObject("PortraitPresentation"); presentation.transform.SetParent(transform, false);
            BuildBoard();
            movement = new ShipMovementController(world.Movement, mapper, shipViews.Values, movementTiming);
            if(saveService==null)toolInventory.ReachLevel(index+1,toolGiftPolicy);
            tools = new ShipToolSystem(session,world.Movement,toolInventory,index+1>=toolGiftPolicy.UnlockLevel);
            progress = new BoardProgressMonitor(session,world.Movement);
            transit=world.Transit;combat=world.Combat;
            BuildControls();
            lane = new LaneTransitController(transit, new PortraitLanePathProvider(session.Width, session.Height),
                shipViews.Values.Select(v => (ILaneTransitView)v.GetComponent<PlanarShipLaneView>()));
            exited.AddRange(world.Capture().Departures.Select(d=>d.Id));
            entered.AddRange(combat.Attacks.Select(t=>t.Ship.ShipId));
            subscriptions.Add(session.Events.Subscribe<ShipExitBoardEvent>(e => exited.Add(e.Ship.ShipId)));
            subscriptions.Add(session.Events.Subscribe<ShipEnterFleetEvent>(e => entered.Add(e.Ship.ShipId)));
            input.Configure(() => session?.Board, ClickShip, NotifyUserActivity, () => IsEntryReady && !IsEntrySaveBlocked);
            foreach(var view in shipViews.Values){view.BindClickHandler(ClickShip);view.SetPaused(IsPaused);}
            lane.PresentActiveTransits();
            notice = saveService!=null && !saveService.IsAvailable ? "Save unavailable. Practice only; account locked." : "Tap a ship to move forward.";
            RefreshViewport(); UpdateLabels();
        }

        public void Restart()
        {
            if (IsEntrySaveBlocked) { RetryEntry(); return; }
            if (entryRequestInFlight || !IsEntryReady) return;
            if(saveService?.IsAvailable!=true){SelectLevel(LevelIndex);return;}
            if(IsCleared && !saveService.CurrentAttemptSettled){SaveCheckpoint(true);UpdateLabels();return;}
            if(IsCleared){if(saveService.CurrentLevel<=catalog.Count)SelectLevel(saveService.CurrentLevel-1);return;}
            var next=SavedGameRuntime.Create(catalog.Load(LevelIndex),LevelIndex+1,transitTiming,combatTiming);
            CommitEntry(next,LevelIndex,true);
        }

        private bool SaveCheckpoint(bool force=false)
        {
            if(IsEntrySaveBlocked)return false;
            if(saveService==null || !saveService.IsAvailable || saveService.Runtime!=world)return true;
            if(saveService.Checkpoint(force))return true;
            demo=null;movement.Pause();notice="Save failed. Resume to retry.";return false;
        }
        private void FlushSave() {if(world!=null)SaveCheckpoint(true);}

        public void ClickShip(string id)
        {
            NotifyUserActivity();
            if (!IsEntryReady || IsEntrySaveBlocked || session == null || IsPaused || IsBusy || demo != null) return;
            if(tools.Selection!=ShipTool.None)
            {
                ApplyToolResult(tools.UseSelected(id));return;
            }
            if(saveService?.IsAvailable==true && !saveService.PrepareMove(id)){notice="Cannot save move. Retry.";UpdateLabels();return;}
            var result = movement.RequestMove(id);
            if (result.IsAccepted) notice = result.Operation.WillExit ? "Clear path - sailing out." :
                result.Operation.TravelDistance > 0 ? "Moves forward, then stops at the blocker." : "Blocked here. Clear the ship ahead.";
            UpdateLabels();
        }

        public void TogglePause()
        {
            NotifyUserActivity();
            if (movement == null || IsEntrySaveBlocked || IsAcquisitionOpen || IsDeadlockOpen) return;
            input.CancelSelection();tools.CancelSelection();
            if(IsPaused) {if(!SaveCheckpoint(true))return;movement.Resume();} else movement.Pause();
            SaveCheckpoint(true);
            UpdateLabels();
        }

        public void SelectTool(ShipTool tool)
        {
            NotifyUserActivity();
            if(!IsEntryReady || IsEntrySaveBlocked || demo!=null || IsMenuOpen || IsAcquisitionOpen || IsDeadlockOpen || IsPaused)return;
            if(tools.Enabled && tools.UsesLeft==0){notice="Tool limit reached. Restart for a new attempt.";UpdateLabels();return;}
            if(tools.Enabled && session.State==GameState.Playing && !IsBusy && toolInventory.IsAvailable && tools.Remaining(tool)==0)
            { ShowAcquisition(tool);return; }
            input.CancelSelection();
            ApplyToolResult(tool==ShipTool.Rescue ? tools.Rescue() : tool==ShipTool.Shuffle ? tools.Shuffle() : tools.Select(tool));
        }
        public void OpenShop() => ShowAcquisition(ShipTool.None);
        private void ShowAcquisition(ShipTool tool)
        {
            NotifyUserActivity();
            if(!IsEntryReady || IsEntrySaveBlocked || !tools.Enabled || IsBusy || demo!=null || IsAcquisitionOpen || IsDeadlockOpen || (session.State!=GameState.Playing && !IsPaused))return;
            input.CancelSelection();tools.CancelSelection();
            // Transfer ownership of a menu-owned pause; an existing user pause remains owned by the user.
            acquisitionPauseOwned=session.State==GameState.Playing || (IsMenuOpen && menuPauseOwned);
            if(IsMenuOpen){menuPanel.gameObject.SetActive(false);menuPauseOwned=false;}
            if(session.State==GameState.Playing)movement.Pause();
            SaveCheckpoint(true);shopPanel.Open(tool);UpdateLabels();
        }
        public void CloseAcquisition()
        {
            if(acquisitionPanel==null)return;
            acquisitionPanel.gameObject.SetActive(false);
            if(acquisitionPauseOwned && IsPaused && SaveCheckpoint(true))movement.Resume();
            acquisitionPauseOwned=false;world.PresentationPause=false;NotifyUserActivity();SaveCheckpoint(true);UpdateLabels();
        }
        public void CancelTool() { NotifyUserActivity();tools.CancelSelection();notice="Tool cancelled.";UpdateLabels(); }
        private void ApplyToolResult(ToolUseStatus result)
        {
            if(result==ToolUseStatus.Applied)
            {
                foreach(var ship in session.Board.Ships)
                {
                    var view=shipViews[ship.Id].transform;view.position=mapper.TailToWorld(ship.Position);
                    view.rotation=Rotation(ship.Direction);view.localScale=Vector3.one;
                }
                ResetBodyColors();
            }
            notice=result==ToolUseStatus.Selected ? "Tap a ship. Tap the tool again to cancel." :
                result==ToolUseStatus.Applied ? "Tool used. Stock saved." :
                result==ToolUseStatus.NoUses ? "No stock. Open the tool shop to buy more." :
                result==ToolUseStatus.StorageUnavailable ? "Cannot save inventory. No tool used." :
                result==ToolUseStatus.Unproven ? "No proven safe result. No use consumed." : "Tool: "+result;
            SaveCheckpoint();progress.Refresh();UpdateLabels();
        }
        public void ToggleMenu()
        {
            NotifyUserActivity();
            if(IsEntrySaveBlocked || IsAcquisitionOpen || IsDeadlockOpen)return;
            if(IsMenuOpen) { CloseMenu();return; }
            input.CancelSelection();tools.CancelSelection();
            menuPauseOwned=session.State==GameState.Playing;
            if(menuPauseOwned)movement.Pause();
            menuPanel.gameObject.SetActive(true);UpdateLabels();
        }
        public void CloseMenu()
        {
            NotifyUserActivity();
            if(menuPanel==null)return;
            menuPanel.gameObject.SetActive(false);
            if(menuPauseOwned && IsPaused)movement.Resume();
            menuPauseOwned=false;UpdateLabels();
        }

        public LevelSolverResult SolveCurrent()
        {
            if (session == null || IsBusy) return null;
            // Current immutable snapshot, never the initial sidecar after a partial move.
            var result = LevelSolver.Solve(session.Board, new LevelSolverOptions(4000, 400000, 150));
            notice = result.Status == LevelSolverStatus.Solved ? "A clear route is available." :
                result.Status == LevelSolverStatus.Deadlocked ? "No complete route. Restart this level." :
                result.Status == LevelSolverStatus.LimitReached ? "Search limit reached. Result is unknown." : "Level state is invalid.";
            UpdateLabels(); return result;
        }

        public void ToggleAuto()
        {
            NotifyUserActivity();
            if(!IsEntryReady || IsEntrySaveBlocked || IsDeadlockOpen || IsAcquisitionOpen)return;
            input.CancelSelection();tools.CancelSelection();
            if (demo != null) { demo = null; notice = "Demo stopped after the current move."; UpdateLabels(); return; }
            if (IsPaused || IsBusy) return;
            var result = SolveCurrent();
            if (result?.Status == LevelSolverStatus.Solved && result.ShipIds.Count > 0)
            { demo = new Queue<string>(result.ShipIds); elapsedSinceDemo = 1; notice = "Auto demo - you can pause or stop."; }
            UpdateLabels();
        }

        public void ShowHint()
        {
            NotifyUserActivity();
            if (!IsEntryReady || IsEntrySaveBlocked || IsPaused || IsBusy || demo != null) return;
            tools.CancelSelection();
            var result = SolveCurrent();
            if (result?.Status != LevelSolverStatus.Solved || result.ShipIds.Count == 0) return;
            ResetBodyColors();
            shipViews[result.ShipIds[0]].transform.Find("Body").GetComponent<Image>().color = new Color(.95f,.72f,.25f);
            notice = "Try the highlighted ship."; UpdateLabels();
        }

        private void Update()
        {
            if (session == null) return;
            if (lastScreen != new Vector2Int(Screen.width, Screen.height) || lastSafe != Screen.safeArea) RefreshViewport();
            if (TickEntry(Time.unscaledDeltaTime)) { UpdateLabels(); return; }
            lane.Advance(Time.unscaledDeltaTime);
            combat.Advance();
            progress.Refresh();
            var durable=SaveCheckpoint();
            if(campaign && saveService?.IsAvailable==true && IsCleared && durable)
            {
                victoryWait+=Time.unscaledDeltaTime;
                if(victoryWait>=1.2f && saveService.CurrentLevel<=catalog.Count){SelectLevel(saveService.CurrentLevel-1);return;}
            }
            if (!IsPaused && demo != null && !IsBusy)
            {
                elapsedSinceDemo += Time.unscaledDeltaTime;
                if (demo.Count == 0) demo = null;
                else if (elapsedSinceDemo >= .08f)
                {
                    var id=demo.Dequeue();
                    if(saveService?.IsAvailable==true && !saveService.PrepareMove(id)){demo=null;notice="Cannot save move. Retry.";UpdateLabels();return;}
                    var request = movement.RequestMove(id); elapsedSinceDemo = 0;
                    if (!request.IsAccepted) { demo = null; notice = "Demo interrupted. Solve the current state again."; }
                }
            }
            TickAssistance(Time.unscaledDeltaTime);
            UpdateLabels();
        }

        public void ApplyViewport(Rect safePixels, float pixelsPerUnit)
        {
            if (session == null || pixelsPerUnit <= 0 || float.IsNaN(pixelsPerUnit) || float.IsInfinity(pixelsPerUnit))
                throw new ArgumentException("Invalid viewport.");
            NotifyUserActivity();input.CancelSelection();
            var safe = new Rect(safePixels.position / pixelsPerUnit, safePixels.size / pixelsPerUnit);
            Layout = PortraitBoardLayout.Calculate(safe, session.Width, session.Height);
            overlay.scaleFactor = pixelsPerUnit;
            Place(topPanel, Layout.Top); Place(toolsPanel, Layout.Tools);
            LayoutControls();
            boardCamera.pixelRect = new Rect(Layout.Middle.position * pixelsPerUnit, Layout.Middle.size * pixelsPerUnit);
            boardCamera.aspect = Layout.Middle.width / Layout.Middle.height;
            boardCamera.orthographicSize = Layout.Middle.height / Layout.CellSize / 2;
            boardCamera.transform.position = new Vector3(session.Width / 2f, session.Height / 2f, -10);
            var thickness = PortraitBoardLayout.LaneWidthInCells;
            Place(lanePanel, new Rect(-thickness, -thickness, session.Width + 2*thickness, session.Height + 2*thickness));
        }

        private void RefreshViewport()
        {
            lastScreen = new Vector2Int(Screen.width, Screen.height); lastSafe = Screen.safeArea;
            // Reference width only normalizes presentation; it is not a physical dp measurement.
            ApplyViewport(lastSafe, Mathf.Max(1, Screen.width) / 390f);
        }

        private void BuildBoard()
        {
            var background = new GameObject("BackgroundCamera", typeof(Camera)); background.transform.SetParent(presentation.transform);
            var clear = background.GetComponent<Camera>(); clear.depth = -10; clear.cullingMask = 0;
            clear.clearFlags = CameraClearFlags.SolidColor; clear.backgroundColor = new Color(.025f,.055f,.08f);
            var cameraObject = new GameObject("BoardCamera", typeof(Camera)); cameraObject.transform.SetParent(presentation.transform);
            boardCamera = cameraObject.GetComponent<Camera>(); boardCamera.orthographic = true;
            boardCamera.clearFlags = CameraClearFlags.SolidColor; boardCamera.backgroundColor = new Color(.035f,.09f,.13f);
            boardCamera.cullingMask = 1 << 30; boardCamera.nearClipPlane = .1f; boardCamera.farClipPlane = 50;
            var canvasRect = Rect("BoardCanvas", presentation.transform);
            var canvas = canvasRect.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = boardCamera; canvasRect.sizeDelta = new Vector2(session.Width,session.Height);
            canvasRect.position = Vector3.zero; canvasRect.localScale = Vector3.one;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            lanePanel = Panel("Lane", canvasRect, new Rect(-.5f,-.5f,session.Width+1,session.Height+1), new Color(.09f,.29f,.36f));
            var grid = Panel("GridInput", canvasRect, new Rect(0,0,session.Width,session.Height), new Color(.055f,.15f,.21f));
            grid.GetComponent<Image>().raycastTarget = true;
            input = grid.gameObject.AddComponent<BoardGridInputRouter>();
            var origin = new GameObject("GridOrigin"); origin.transform.SetParent(presentation.transform); origin.transform.position = new Vector3(.5f,.5f,0);
            mapper = origin.AddComponent<GridWorldMapper>(); mapper.Configure(origin.transform,1,Vector3.right,Vector3.up);
            input.ConfigureMapping(boardCamera,mapper);
            foreach (var ship in session.Ships)
            {
                var root = Rect("Ship_"+ship.Id, canvasRect); root.sizeDelta = Vector2.one;
                root.position = mapper.TailToWorld(ship.Position);
                root.rotation = Rotation(ship.Direction);
                var body = Panel("Body", root, new Rect(-.38f,-.43f,.76f,ship.Length-.14f), BodyColor(ship.Length));
                var arrow = Rect("Direction", body); Place(arrow,new Rect(.09f,ship.Length-1.00f,.58f,.7f));
                var graphic = arrow.gameObject.AddComponent<GrayboxArrowGraphic>(); graphic.color = Color.white; graphic.raycastTarget = false;
                var view = root.gameObject.AddComponent<ShipMovementView>(); view.ConfigureShipId(ship.Id);
                root.gameObject.AddComponent<PlanarShipLaneView>().Configure(ship.Id, Vector3.up * ((ship.Length - 1) * .5f)); shipViews.Add(ship.Id,view);
                root.gameObject.SetActive(ship.State!=ShipState.InFleet);
            }
            foreach (var child in canvasRect.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        }

        private void BuildControls()
        {
            var canvasRect = Rect("GrayboxControls", presentation.transform);
            overlay = canvasRect.gameObject.AddComponent<Canvas>(); overlay.renderMode = RenderMode.ScreenSpaceOverlay; overlay.sortingOrder = 10;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            topPanel = Panel("Top", canvasRect, new Rect(), new Color(.055f,.12f,.18f));
            toolsPanel = Panel("Tools", canvasRect, new Rect(), new Color(.055f,.12f,.18f));
            Button("Menu",topPanel,"Menu",ToggleMenu);
            Button("Next",topPanel,">",() => SelectLevel((LevelIndex+1)%catalog.Count));
            pause = Button("Pause",topPanel,"Pause",TogglePause);
            title = Label("Title",topPanel,"",19);
            wallet=Label("Wallet",topPanel,"",12);
            battlePanel = Panel("Battle",topPanel,new Rect(),new Color(.055f,.12f,.18f));
            combatView = battlePanel.gameObject.AddComponent<FleetCombatGrayboxView>();
            combatView.Initialize(session,combat,font);
            Button("Restart",toolsPanel,"Restart",Restart);
            hint = Button("Hint",toolsPanel,"Hint",ShowHint);
            auto = Button("Auto",toolsPanel,"Auto",ToggleAuto);
            status = Label("Status",toolsPanel,"",12);
            rescueButton=Button("Rescue",toolsPanel,"",()=>SelectTool(ShipTool.Rescue));
            shuffleButton=Button("Shuffle",toolsPanel,"",()=>SelectTool(ShipTool.Shuffle));
            reverseButton=Button("Reverse",toolsPanel,"",()=>SelectTool(ShipTool.Reverse));
            foreach(var name in new[]{"Restart","Hint","Auto"})toolsPanel.Find(name).gameObject.SetActive(!tools.Enabled);
            foreach(var button in new[]{rescueButton,shuffleButton,reverseButton})button.gameObject.SetActive(tools.Enabled);
            menuPanel=Panel("PrototypeMenu",canvasRect,new Rect(),new Color(.025f,.055f,.08f,.97f));
            menuPanel.GetComponent<Image>().raycastTarget=true;
            Label("MenuTitle",menuPanel,"Prototype controls",20);
            Button("Previous",menuPanel,"Previous",()=>{CloseMenu();SelectLevel((LevelIndex+catalog.Count-1)%catalog.Count);});
            Button("Restart",menuPanel,"Restart",()=>{CloseMenu();Restart();});
            Button("Hint",menuPanel,"Hint",()=>{CloseMenu();ShowHint();});
            Button("Auto",menuPanel,"Auto / Stop",()=>{CloseMenu();ToggleAuto();});
            Button("Next",menuPanel,"Next",()=>{CloseMenu();SelectLevel((LevelIndex+1)%catalog.Count);});
            Button("Continue",menuPanel,"Continue",CloseMenu);
            Button("Shop",menuPanel,"Tool shop",OpenShop);
            if(saveService!=null)
            {
                topPanel.Find("Next").gameObject.SetActive(false);
                menuPanel.Find("Previous").gameObject.SetActive(false);menuPanel.Find("Next").gameObject.SetActive(false);
            }
            menuPanel.gameObject.SetActive(false);
            acquisitionPanel=Panel("ToolAcquisition",canvasRect,new Rect(),new Color(.025f,.055f,.08f,.97f));
            acquisitionPanel.GetComponent<Image>().raycastTarget=true;
            shopPanel=acquisitionPanel.gameObject.AddComponent<CoinShopPanel>();
            shopPanel.Initialize(saveService,toolInventory,CoinShopCatalogReader.LoadDefault(),font,CloseAcquisition,()=>tools.UsesLeft);
            acquisitionPanel.gameObject.SetActive(false);
            BuildAssistance(canvasRect);BuildEntryControls(canvasRect);
            if (FindObjectOfType<EventSystem>() == null)
            {
                var events = new GameObject("GrayboxEventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
                events.transform.SetParent(presentation.transform);
            }
        }

        private void LayoutControls()
        {
            var w = Layout.Top.width; var h = Layout.Top.height;
            Place((RectTransform)topPanel.Find("Menu"),new Rect(0,h-48,48,48));
            Place((RectTransform)topPanel.Find("Next"),new Rect(w-116,h-48,48,48));
            Place((RectTransform)pause.transform,new Rect(w-64,h-48,64,48));
            Place(title.rectTransform,new Rect(52,h-48,w-172,48));
            Place(wallet.rectTransform,new Rect(8,h-70,w-16,20));
            Place(battlePanel,new Rect(8,4,w-16,h-78));
            var buttonWidth = (w-32)/3;
            var names = new[] { "Restart", "Hint", "Auto" };
            Place(status.rectTransform,new Rect(8,Layout.Tools.height-32,w-16,32));
            for (var i=0;i<3;i++) Place((RectTransform)toolsPanel.Find(names[i]),new Rect(8+i*(buttonWidth+8),4,buttonWidth,Mathf.Max(48,Layout.Tools.height-40)));
            var toolNames=new[]{"Rescue","Shuffle","Reverse"};
            for(var i=0;i<3;i++)Place((RectTransform)toolsPanel.Find(toolNames[i]),new Rect(8+i*(buttonWidth+8),4,buttonWidth,Mathf.Max(48,Layout.Tools.height-40)));
            Place(menuPanel,Layout.Safe);Place(acquisitionPanel,Layout.Safe);
            shopPanel.Layout(Layout.Safe);deadlockView.Layout(Layout.Safe);LayoutEntryControls();
            var menuWidth=(w-40)/2;var menuY=Layout.Safe.height/2-84;
            Place((RectTransform)menuPanel.Find("MenuTitle"),new Rect(8,menuY+176,w-16,32));
            Place((RectTransform)menuPanel.Find("Shop"),new Rect(16,menuY-56,w-32,48));
            Place((RectTransform)menuPanel.Find("AutoHints"),new Rect(16,menuY-112,w-32,48));
            Place((RectTransform)menuPanel.Find("HintMotion"),new Rect(16,menuY-168,w-32,48));
            var menuNames=new[]{"Previous","Next","Hint","Auto","Restart","Continue"};
            for(var i=0;i<6;i++)Place((RectTransform)menuPanel.Find(menuNames[i]),new Rect(16+(i%2)*(menuWidth+8),menuY+(2-i/2)*56,menuWidth,48));
        }

        private void UpdateLabels()
        {
            if (title == null || session == null) return;
            title.text = "LEVEL " + (LevelIndex+1);
            wallet.text=saveService==null ? "Review mode - rewards are not saved" : !saveService.IsAvailable ? "Practice - save unavailable" :
                "Coins: "+saveService.Coins+"   |   Pending: "+(saveService.CurrentAttemptSettled ? 0 : world.PendingCoins);
            if(tools.Enabled)wallet.text+="   |   Tools: "+tools.UsesLeft+"/"+ShipToolSystem.MaxUsesPerAttempt;
            var deadlocked=!IsBusy && world.PendingMoveId==null && progress.Refresh()==BoardProgressStatus.NoMoves;
            var restartLabel=saveService?.IsAvailable==true && deadlocked ? (saveService.RestartReward>0 ? "Collect "+saveService.RestartReward+" & restart" : "Restart (0 coins)") : "Restart";
            if(saveService?.IsAvailable==true && IsCleared)restartLabel=saveService.CurrentLevel<=catalog.Count ? "Next level" : "Complete";
            foreach(var parent in new[]{menuPanel,toolsPanel})
            {
                var button=parent.Find("Restart").GetComponent<Button>();button.GetComponentInChildren<Text>(true).text=restartLabel;
                button.interactable=IsEntryReady && !IsEntrySaveBlocked && !(saveService?.IsAvailable==true && IsCleared && saveService.CurrentLevel>catalog.Count);
            }
            menuPanel.Find("Shop").GetComponent<Button>().interactable=IsEntryReady && !IsEntrySaveBlocked && tools.Enabled && !IsBusy && demo==null && (session.State==GameState.Playing || IsPaused);
            if(IsAcquisitionOpen)shopPanel.Present();
            combatView.Present();
            status.text = IsCleared ? "VICTORY - all ships fired." : combat.FaultReason!=null ? "Combat error: "+combat.FaultReason : IsPaused ? "Paused" :
                tools.Selection!=ShipTool.None ? notice : !IsBusy && progress.NeedsRescue ? "No solution. Use a tool or Menu > Restart." : notice;
            if(saveService?.LastError!=null && saveService.IsAvailable)status.text="Save failed. Resume / Restart to retry.";
            else if(IsCleared && saveService!=null && saveService.CurrentLevel>catalog.Count)status.text="All available levels complete. Progress saved.";
            else if(deadlocked && saveService?.IsAvailable==true)status.text="No moves. Restart reward: "+saveService.RestartReward+" coins. Tools available.";
            pause.interactable = !IsEntrySaveBlocked && (session.State==GameState.Playing || IsPaused);
            pause.GetComponentInChildren<Text>(true).text = IsPaused ? "Resume" : "Pause";
            auto.GetComponentInChildren<Text>(true).text = demo != null ? "Stop" : "Auto";
            auto.interactable = IsEntryReady && !IsEntrySaveBlocked && (demo != null || (!IsPaused && !IsBusy && !IsCleared));
            hint.interactable = IsEntryReady && !IsEntrySaveBlocked && session.State==GameState.Playing && !IsBusy && demo == null && session.Board.ShipCount>0;
            var toolKinds=new[]{ShipTool.Rescue,ShipTool.Shuffle,ShipTool.Reverse};var buttons=new[]{rescueButton,shuffleButton,reverseButton};
            var labels=new[]{"Rescue 2","Shuffle 5","Reverse"};
            for(var i=0;i<3;i++)
            {
                var selected=tools.Selection==toolKinds[i];
                buttons[i].GetComponentInChildren<Text>(true).text=selected ? "Cancel" : labels[i]+" ("+tools.Remaining(toolKinds[i])+")";
                buttons[i].interactable=IsEntryReady && !IsEntrySaveBlocked && tools.Enabled && tools.UsesLeft>0 && session.State==GameState.Playing && !IsBusy && demo==null && session.Board.ShipCount>0;
            }
            UpdateAssistanceLabels();UpdateEntryLabels();
        }

        private void ResetBodyColors()
        {
            foreach (var ship in session.Ships)
                shipViews[ship.Id].transform.Find("Body").GetComponent<Image>().color = BodyColor(ship.Length);
        }
        private static Color BodyColor(int length) => length==3 ? new Color(.40f,.57f,.67f) : new Color(.18f,.52f,.65f);
        private static Quaternion Rotation(ShipDirection direction) => Quaternion.Euler(0,0,
            direction==ShipDirection.Up ? 0 : direction==ShipDirection.Down ? 180 : direction==ShipDirection.Left ? 90 : -90);

        private RectTransform Rect(string name, Transform parent)
        {
            var value = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            value.SetParent(parent,false); value.anchorMin = value.anchorMax = value.pivot = Vector2.zero; return value;
        }
        private RectTransform Panel(string name, Transform parent, Rect bounds, Color color)
        {
            var r = Rect(name,parent); Place(r,bounds); var image = r.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return r;
        }
        private Text Label(string name, Transform parent, string value, int size)
        {
            var r=Rect(name,parent); var t=r.gameObject.AddComponent<Text>(); t.font=font; t.fontSize=size; t.text=value;
            t.alignment=TextAnchor.MiddleCenter; t.color=Color.white; t.raycastTarget=false; return t;
        }
        private Button Button(string name, Transform parent, string label, Action clicked)
        {
            var r=Panel(name,parent,new Rect(),new Color(.13f,.27f,.35f)); var image=r.GetComponent<Image>(); image.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=image;
            if (clicked!=null) b.onClick.AddListener(() => { NotifyUserActivity(); clicked(); });
            var text=Label("Label",r,label,14); text.rectTransform.anchorMax=Vector2.one;
            text.rectTransform.offsetMin=Vector2.zero; text.rectTransform.offsetMax=Vector2.zero;
            return b;
        }
        private static void Place(RectTransform target, Rect r) { target.anchoredPosition=r.position; target.sizeDelta=r.size; }

        private void ClearSession()
        {
            ClearEntry();ClearAutoHighlight();assistance=null;deadlockPanel=null;deadlockView=null;deadlockPauseOwned=false;inputSinceTick=false;
            demo=null; input?.CancelSelection(); movement?.Dispose(); movement=null;
            tools?.Dispose();tools=null;progress=null;menuPanel=null;menuPauseOwned=false;acquisitionPanel=null;shopPanel=null;acquisitionPauseOwned=false;
            combat=null; combatView=null;
            lane?.Dispose(); lane=null; transit=null;
            foreach (var subscription in subscriptions) subscription.Dispose(); subscriptions.Clear();
            world?.Dispose();world=null;session=null; shipViews.Clear(); exited.Clear(); entered.Clear();
            if (presentation!=null) { presentation.SetActive(false); Destroy(presentation); } presentation=null;
        }
        private void OnApplicationPause(bool paused) {backgrounded=paused;NotifyUserActivity();if(paused && world!=null){if(!IsPaused)movement.Pause();FlushSave();}}
        private void OnApplicationFocus(bool focused) {backgrounded=!focused;NotifyUserActivity();}
        private void OnApplicationQuit() => FlushSave();
        private void OnDestroy() {FlushSave();ClearSession();}

    }
}
