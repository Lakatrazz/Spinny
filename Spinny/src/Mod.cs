using BoneLib;
using BoneLib.BoneMenu;

using Il2CppSLZ.Marrow;

using MelonLoader;

using UnityEngine;

using Page = BoneLib.BoneMenu.Page;

namespace Spinny;

public class SpinnyMod : MelonMod 
{
    public const string Name = "Spinny";
    public const string Author = "Lakatrazz";
    public const string Description = "Turns you with what you stand on or grab.";
    public const string Version = "1.0.0";

    public static MelonPreferences_Category MelonPrefCategory { get; private set; }
    public static MelonPreferences_Entry<bool> MelonPrefEnabled { get; private set; }
    public static MelonPreferences_Entry<bool> MelonPrefGroundRotation { get; private set; }
    public static MelonPreferences_Entry<bool> MelonPrefGrabRotation { get; private set; }
    public static MelonPreferences_Entry<bool> MelonPrefBeingGrabbedRotation { get; private set; }

    public static bool Enabled { get; private set; }
    public static bool GroundRotation { get; private set; }
    public static bool GrabRotation { get; private set; }
    public static bool BeingGrabbedRotation { get; private set; }

    public static Page MainPage { get; private set; }
    public static BoolElement EnabledElement { get; private set; }
    public static BoolElement GroundRotationElement { get; private set; }
    public static BoolElement GrabRotationElement { get; private set; }
    public static BoolElement BeingGrabbedRotationElement { get; private set; }

    private static Grip[] _playerGrips = System.Array.Empty<Grip>();

    private static bool _preferencesSetup = false;

    private static float _targetSpin = 0f;
    private static float _currentSpin = 0f;

    public override void OnInitializeMelon() 
    {
        Hooking.OnLevelLoaded += OnLevelLoaded;

        SetupMelonPrefs();
        SetupBoneMenu();
    }

    private void OnLevelLoaded(LevelInfo info)
    {
        var physicsRig = Player.PhysicsRig;

        var torso = physicsRig.torso;

        var leftHand = physicsRig.leftHand.physHand;
        var rightHand = physicsRig.rightHand.physHand;

        _playerGrips = new Grip[]
        {
            torso.gChest,
            torso.gHead,
            torso.gNeck,
            torso.gPelvis,
            torso.gSpine,

            leftHand.gShoulder,
            leftHand.gElbow,

            rightHand.gShoulder,
            rightHand.gElbow,
        };
    }

    public static void SetupMelonPrefs() 
    {
        MelonPrefCategory = MelonPreferences.CreateCategory("Spinny");
        MelonPrefEnabled = MelonPrefCategory.CreateEntry("Enabled", true);
        MelonPrefGroundRotation = MelonPrefCategory.CreateEntry("Ground Rotation", true);
        MelonPrefGrabRotation = MelonPrefCategory.CreateEntry("Grab Rotation", true);
        MelonPrefBeingGrabbedRotation = MelonPrefCategory.CreateEntry("Being Grabbed Rotation", true);

        Enabled = MelonPrefEnabled.Value;
        GroundRotation = MelonPrefGroundRotation.Value;
        GrabRotation = MelonPrefGrabRotation.Value;
        BeingGrabbedRotation = MelonPrefBeingGrabbedRotation.Value;

        _preferencesSetup = true;
    }

    public static void SetupBoneMenu()
    {
        MainPage = Page.Root.CreatePage("Spinny", Color.green);
        EnabledElement = MainPage.CreateBool("Enabled", Color.yellow, Enabled, OnSetEnabled);
        GroundRotationElement = MainPage.CreateBool("Ground Rotation", Color.yellow, GroundRotation, OnSetGroundRotation);
        GrabRotationElement = MainPage.CreateBool("Grab Rotation", Color.yellow, GrabRotation, OnSetGrabRotation);
        BeingGrabbedRotationElement = MainPage.CreateBool("Being Grabbed Rotation", Color.yellow, BeingGrabbedRotation, OnSetBeingGrabbedRotation);
    }

    private static void OnSetEnabled(bool value) 
    {
        Enabled = value;
        MelonPrefEnabled.Value = value;
        MelonPrefCategory.SaveToFile(false);
    }

    private static void OnSetGroundRotation(bool value)
    {
        GroundRotation = value;
        MelonPrefGroundRotation.Value = value;
        MelonPrefCategory.SaveToFile(false);
    }

    private static void OnSetGrabRotation(bool value)
    {
        GrabRotation = value;
        MelonPrefGrabRotation.Value = value;
        MelonPrefCategory.SaveToFile(false);
    }

    private static void OnSetBeingGrabbedRotation(bool value)
    {
        BeingGrabbedRotation = value;
        MelonPrefBeingGrabbedRotation.Value = value;
        MelonPrefCategory.SaveToFile(false);
    }

    public override void OnPreferencesLoaded() 
    {
        if (!_preferencesSetup)
        {
            return;
        }

        Enabled = MelonPrefEnabled.Value;
        GroundRotation = MelonPrefGroundRotation.Value;
        GrabRotation = MelonPrefGrabRotation.Value;

        EnabledElement.Value = Enabled;
        GroundRotationElement.Value = GroundRotation;
        GrabRotationElement.Value = GrabRotation;
    }

    public override void OnUpdate()
    {
        if (!Enabled)
        {
            return;
        }

        var rig = Player.RigManager;

        if (rig == null)
        {
            return;
        }

        var physicsRig = rig.physicsRig;

        if (!physicsRig.ballLocoEnabled)
        {
            _targetSpin = 0f;
            _currentSpin = 0f;
            return;
        }

        float friction = 12f;

        _targetSpin = 0f;

        bool grounded = CheckGrounded(physicsRig);

        if (grounded)
        {
            if (GroundRotation)
            {
                SolveGroundRotation(physicsRig);
            }
        }
        else if (BeingGrabbedRotation && CheckBeingGrabbed())
        {
            SolveBeingGrabbedRotation();
        }
        else if (GrabRotation && CheckGrabbing(physicsRig))
        {
            SolveGrabRotation(physicsRig);
        }
        else
        {
            friction = 2f;
        }

        _currentSpin = Mathf.Lerp(_currentSpin, _targetSpin, Smoothing.CalculateDecay(friction, Time.deltaTime));

        rig.remapHeptaRig.SetTwist(_currentSpin * Mathf.Rad2Deg);
    }

    private static bool CheckGrounded(PhysicsRig physicsRig)
    {
        return physicsRig.physG.isGrounded;
    }

    private static bool CheckGrabbing(PhysicsRig physicsRig)
    {
        return physicsRig.leftHand.m_CurrentAttachedGO || physicsRig.rightHand.m_CurrentAttachedGO;
    }

    private static bool CheckBeingGrabbed()
    {
        foreach (var grip in _playerGrips)
        {
            if (grip.HasAttachedHands())
            {
                return true;
            }
        }

        return false;
    }

    private static void SolveGroundRotation(PhysicsRig physicsRig)
    {
        float radians = physicsRig.groundAngVelocity * Time.deltaTime;

        var collider = physicsRig.physG._groundedCollider;

        if (collider != null && collider.attachedRigidbody)
        {
            radians *= CalculateMassSupport(collider.attachedRigidbody);
        }

        _targetSpin = radians;
    }

    private static void SolveBeingGrabbedRotation()
    {
        float totalAngularVelocity = 0f;
        int grabbingGrips = 0;

        foreach (var grip in _playerGrips)
        {
            if (!grip.HasAttachedHands())
            {
                continue;
            }

            totalAngularVelocity += GetBeingGrabbedAngularVelocity(grip);
            grabbingGrips++;
        }

        if (grabbingGrips <= 0)
        {
            return;
        }

        totalAngularVelocity /= grabbingGrips;

        _targetSpin = totalAngularVelocity * Time.deltaTime;
    }

    private static float GetBeingGrabbedAngularVelocity(Grip grip)
    {
        float selfArmMass = Player.RigManager.avatar.massArm;

        float totalAngularVelocity = 0f;
        int totalHands = 0;

        foreach (var hand in grip.attachedHands)
        {
            var physHand = hand.physHand;

            var armMass = physHand._upperArmRb.mass + physHand._lowerArmRb.mass + physHand._handRb.mass;

            float massPercent = Mathf.Clamp01(armMass / (selfArmMass * 2f));

            totalAngularVelocity += hand.physHand._upperArmRb.angularVelocity.y * massPercent;
            totalHands++;
        }

        totalAngularVelocity /= totalHands;

        return totalAngularVelocity;
    }

    private static void SolveGrabRotation(PhysicsRig physicsRig)
    {
        var leftContribution = GetGrabContribution(physicsRig.leftHand);
        var rightContribution = GetGrabContribution(physicsRig.rightHand);

        var total = leftContribution.supported + rightContribution.supported;

        if (total <= 0f)
        {
            return;
        }

        float leftPercent = leftContribution.supported / total;
        float rightPercent = rightContribution.supported / total;

        var leftVelocity = leftContribution.angularVelocity * leftPercent;
        var rightVelocity = rightContribution.angularVelocity * rightPercent;

        var angularVelocity = leftVelocity + rightVelocity;

        float radians = angularVelocity * Time.deltaTime;

        _targetSpin = radians;

        // Inverse forces
        if (leftContribution.rigidbody)
        {
            leftContribution.rigidbody.AddTorque(Vector3.up * -leftVelocity * 0.2f, ForceMode.VelocityChange);
        }

        if (rightContribution.rigidbody)
        {
            rightContribution.rigidbody.AddTorque(Vector3.up * -rightVelocity * 0.2f, ForceMode.VelocityChange);
        }
    }

    private static (float supported, float angularVelocity, Rigidbody rigidbody) GetGrabContribution(Hand hand)
    {
        if (hand.m_CurrentAttachedGO == null)
        {
            return (0f, 0f, null);
        }

        var grip = Grip.Cache.Get(hand.m_CurrentAttachedGO);

        if (grip == null) 
        {
            return (0f, 0f, null);
        }

        var host = grip.Host;

        if (host == null || !host.HasRigidbody)
        {
            return (0f, 0f, null);
        }

        var rb = host.Rb;

        var angularVelocity = rb.angularVelocity.y;

        var massSupport = CalculateMassSupport(rb);

        float handSupport = Mathf.Abs(hand.physHand.handSupported) * massSupport;

        angularVelocity *= massSupport;

        return (handSupport, angularVelocity, rb);
    }

    private static float CalculateMassSupport(Rigidbody rigidbody)
    {
        var playerAvatar = Player.RigManager.avatar;
        float playerMass = playerAvatar.massPelvis;

        var angularVelocity = rigidbody.angularVelocity.y;

        var massPercent = Mathf.Pow(rigidbody.mass / playerMass, 4f);

        var velocityPercent = Mathf.Abs(angularVelocity) / 10f + 1f;
        velocityPercent *= velocityPercent;

        return Mathf.Clamp01(massPercent * velocityPercent);
    }
}