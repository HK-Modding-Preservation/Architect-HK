using System.Collections;
using System.Collections.Generic;
using Architect.Behaviour.Utility;
using Architect.Content.Custom;
using UnityEngine;

namespace Architect.Behaviour.Custom;

public class Feather : PreviewableBehaviour
{
    private const string FEATHER_FLYING_SOURCE = "FeatherFlying";

    private static readonly List<Sprite> Sprites = [];
    private static readonly List<Sprite> BreakAnim = [];

    private static float _remainingTime;
    private static float _timeSincePush;
    private static GameObject _comet;
    private static Rigidbody2D _cometBody;
    private static CircleCollider2D _cometCollider;
    private static SpriteRenderer _cometRenderer;
    private static ParticleSystemRenderer _cometParticleRenderer;
    private static bool _cometValid;

    private static AudioClip _obtainFeather;
    private static AudioClip _renewFeather;
    private static AudioClip _loseFeather;
    private static AudioClip _loop;

    public bool legacy;

    private static BoxCollider2D _heroBox;

    private static bool _regainControl;
    private static bool _show;

    private static AudioSource _source;

    private static readonly Sprite Comet = ResourceUtils.LoadSpriteResource(
        "Feather.comet", FilterMode.Point, ppu: 10);

    private static readonly Sprite CometBlink = ResourceUtils.LoadSpriteResource(
        "Feather.comet_blink", FilterMode.Point, ppu: 10);

    private static readonly Material CometTrail = new(Shader.Find("Sprites/Default"))
    {
        mainTexture = ResourceUtils.LoadSpriteResource("Feather.comet_trail", FilterMode.Point).texture
    };

    private static readonly Material CometBlinkTrail = new(Shader.Find("Sprites/Default"))
    {
        mainTexture = ResourceUtils.LoadSpriteResource("Feather.comet_blink_trail", FilterMode.Point).texture
    };

    public float featherTime = 5;
    public float respawnTime = 6;
    private float _dt;
    private bool _enabled = true;
    private int _spriteIndex = 2;

    private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        var sounds = HeroController.instance.transform.Find("Sounds");

        _source = sounds.Find(FEATHER_FLYING_SOURCE)?.GetComponent<AudioSource>();
        if (!_source)
            _source = new GameObject(FEATHER_FLYING_SOURCE)
            {
                transform = { parent = sounds }
            }.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (!_enabled) return;
        _dt += Time.deltaTime * 15;
        while (_dt > 1)
        {
            _spriteIndex = (_spriteIndex + 1) % Sprites.Count;
            _dt--;
        }

        _spriteRenderer.sprite = Sprites[_spriteIndex];
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isAPreview) return;
        if (!_enabled) return;
        if (!other.GetComponent<HeroController>()) return;

        _enabled = false;
        _spriteIndex = 2;
        _dt = 0;

        AbilityObjects.RechargeShadowDash();
        StartCoroutine(legacy ? FlyLegacy() : Fly());
        StartCoroutine(Respawn());
    }

    public static void Init()
    {
        ResourceUtils.LoadClipResource("Feather.feather_get", clip => _obtainFeather = clip);
        ResourceUtils.LoadClipResource("Feather.feather_renew", clip => _renewFeather = clip);
        ResourceUtils.LoadClipResource("Feather.feather_state_end", clip => _loseFeather = clip);
        ResourceUtils.LoadClipResource("Feather.feather_state_fast_loop", clip => _loop = clip);
        
        SetupSprites("Feather.flash.f");
        SetupSprites("Feather.loop.f");
        SetupBreakAnim();

        _comet = new GameObject("Comet");
        _cometRenderer = _comet.AddComponent<SpriteRenderer>();
        _cometRenderer.sprite = Comet;
        _comet.SetActive(false);

        _cometCollider = _comet.AddComponent<CircleCollider2D>();
        _cometCollider.radius = 0.25f;
        _cometCollider.offset = Vector2.zero;

        _cometBody = _comet.AddComponent<Rigidbody2D>();
        _cometBody.bodyType = RigidbodyType2D.Dynamic;
        _cometBody.gravityScale = 0;

        _comet.AddComponent<CometMovement>();

        _comet.layer = LayerMask.NameToLayer("Player");

        var child = new GameObject("Particle System")
        {
            transform =
            {
                parent = _comet.transform,
                position = new Vector3(0, 0, -0.1f),
                localScale = new Vector2(0.2f, 0.2f)
            }
        };

        var ps = child.AddComponent<ParticleSystem>();
        _cometParticleRenderer = ps.GetComponent<ParticleSystemRenderer>();
        _cometParticleRenderer.material = CometTrail;

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeedMultiplier = 40;
        main.startLifetimeMultiplier /= 10;

        var emission = ps.emission;
        emission.rateOverTimeMultiplier *= 20;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 1, 0.5f, 0));

        DontDestroyOnLoad(_comet);

        ModHooks.AfterTakeDamageHook += (type, amount) =>
        {
            if (_cometValid)
            {
                _cometValid = false;
                _regainControl = false;
                if (type != 1) _show = false;
            }

            return amount;
        };

        On.HeroController.Awake += (orig, self) =>
        {
            orig(self);
            Physics2D.IgnoreCollision(self.col2d, _cometCollider);
            _heroBox = self.transform.Find("HeroBox").GetComponent<BoxCollider2D>();
        };

        On.HeroController.SceneInit += (orig, self) =>
        {
            self.renderer.enabled = true;
            ResetHitboxes();
            orig(self);
        };
    }

    private static void ResetHitboxes()
    {
        _heroBox.offset = new Vector2(0.0056f, -0.7031f);
        _heroBox.size = new Vector2(0.4554f, 1.1875f);
    }

    private static void SetupSprites(string name)
    {
        for (var i = 0; i <= 20; i++) Sprites.Add(ResourceUtils.LoadSpriteResource(name + i, FilterMode.Point, ppu: 10));
    }

    private static void SetupBreakAnim()
    {
        for (var i = 0; i <= 5; i++)
            BreakAnim.Add(ResourceUtils.LoadSpriteResource("Feather.break.f" + i, FilterMode.Point, ppu: 10));
    }

    public void Setup()
    {
        gameObject.AddComponent<SpriteRenderer>().sprite = Sprites[0];
    }

    private static IEnumerator StartSound()
    {
        var maxVol = GameManager.instance.GetImplicitCinematicVolume() / 5;

        _source.volume = maxVol;
        _source.time = 0;
        _source.clip = _obtainFeather;
        _source.loop = false;
        _source.Play();

        while (_source.isPlaying) yield return null;

        _source.volume = 0;
        _source.clip = _loop;
        _source.loop = true;
        _source.Play();

        var paused = false;
        while (_source.volume < maxVol)
        {
            if (GameManager.instance.isPaused)
            {
                if (!paused) _source.Pause();
                paused = true;
                yield return null;
            }
            else if (paused)
            {
                paused = false;
                _source.Play();
            }

            if (!_cometValid) yield break;
            _source.volume = Mathf.Lerp(_source.volume, maxVol, Time.deltaTime);
            yield return null;
        }
    }
    
    private static readonly LayerMask TerrainMask = LayerMask.GetMask("Terrain");

    private IEnumerator Fly()
    {
        var hero = HeroController.instance;

        _remainingTime = Mathf.Max(_remainingTime, featherTime);
        if (!_cometValid) _timeSincePush = 0;
        _cometParticleRenderer.material = CometTrail;
        _cometRenderer.sprite = Comet;

        if (hero.controlReqlinquished)
        {
            _source.PlayOneShot(_renewFeather);
            yield break;
        }

        gameObject.BroadcastEvent("OnActivate");

        _heroBox.offset = new Vector2(0, -1);
        _heroBox.size = new Vector2(0.5f, 0.5f);

        StartCoroutine(StartSound());

        _comet.SetActive(true);

        var rb2d = hero.rb2d;

        _regainControl = true;
        _show = true;

        _comet.transform.position = transform.position;
        _cometBody.velocity = rb2d.velocity / 3;
        _cometValid = true;

        hero.renderer.enabled = false;
        hero.RelinquishControl();

        var up = InputHandler.Instance.inputActions.up;
        var down = InputHandler.Instance.inputActions.down;
        var left = InputHandler.Instance.inputActions.left;
        var right = InputHandler.Instance.inputActions.right;

        _comet.transform.SetRotation2D(hero.cState.facingRight ? 0 : 180);
        _comet.transform.SetRotation2D(GetInputs().Item1);

        var dashed = false;

        while (_remainingTime > 0)
        {
            if (_cometValid && rb2d.gravityScale == 0)
            {
                _regainControl = false;
                _show = false;
                _cometValid = false;
            }

            if (!_cometValid) break;

            _remainingTime -= Time.deltaTime;
            _timeSincePush += Time.deltaTime;

            var (target, pushing) = GetInputs();
            
            _comet.transform.rotation = Quaternion.Lerp(_comet.transform.rotation, Quaternion.Euler(0, 0, target), Mathf.Clamp01(Time.deltaTime * 3.25f));

            rb2d.velocity = Vector2.zero;
            hero.transform.position = _comet.transform.position + new Vector3(0, 1);

            if (pushing) _timeSincePush = 0;
            _cometBody.velocity = _comet.transform.right * Mathf.Max(8, 15 - _timeSincePush * 15);

            if (!GameManager.instance.isPaused && InputHandler.Instance.inputActions.dash.WasPressed)
            {
                dashed = true;
                hero.SetStartWithDash();
                break;
            }

            yield return null;
        }

        if (_regainControl) hero.RegainControl();
        if (_show) hero.renderer.enabled = true;

        if (!dashed) hero.airDashed = false;
        hero.doubleJumped = false;

        _comet.SetActive(false);
        _cometValid = false;

        _source.Stop();
        _source.PlayOneShot(_loseFeather);

        _remainingTime = 0;

        gameObject.BroadcastEvent("OnFinish");
        ResetHitboxes();

        yield break;

        (float, bool) GetInputs()
        {
            var target = _comet.transform.GetRotation2D();

            var l = left.IsPressed;
            var r = right.IsPressed;
            var u = up.IsPressed;
            var d = down.IsPressed;

            var pushing = l || r || u || d;

            const float dist = 0.35f;

            var downCast = Physics2D.Raycast(_comet.transform.position, Vector2.down, dist, TerrainMask);
            var upCast = Physics2D.Raycast(_comet.transform.position, Vector2.up, dist, TerrainMask);
            var leftCast = Physics2D.Raycast(_comet.transform.position, Vector2.left, dist, TerrainMask);
            var rightCast = Physics2D.Raycast(_comet.transform.position, Vector2.right, dist, TerrainMask);
            
            if (downCast)
            {
                d = false;
                u = !upCast;
            }
            else if (upCast)
            {
                u = false;
                d = !downCast;
            }

            if (leftCast)
            {
                l = false;
                r = !rightCast;
            }
            else if (rightCast)
            {
                r = false;
                l = !leftCast;
            }
            
            if (l && !r)
            {
                hero.FaceLeft();
                target = 180;
            }
            else if (r && !l)
            {
                hero.FaceRight();
                target = 0;
            }

            if (u && !d)
            {
                if (l && !r) target = 135;
                else if (r && !l) target = 45;
                else target = 90;
            }
            else if (d && !u)
            {
                if (l && !r) target = -135;
                else if (r && !l) target = -45;
                else target = -90;
            }
            
            return (target, pushing);
        }
    }
    
    private IEnumerator FlyLegacy()
    {
        var hero = HeroController.instance;

        _remainingTime = Mathf.Max(_remainingTime, featherTime);
        _cometParticleRenderer.material = CometTrail;
        _cometRenderer.sprite = Comet;

        if (hero.controlReqlinquished)
        {
            _source.PlayOneShot(_renewFeather);
            yield break;
        }

        gameObject.BroadcastEvent("OnActivate");

        _heroBox.offset = new Vector2(0, -1);
        _heroBox.size = new Vector2(0.5f, 0.5f);

        StartCoroutine(StartSound());

        _comet.SetActive(true);

        var rb2d = hero.GetComponent<Rigidbody2D>();

        _regainControl = true;
        _show = true;

        _comet.transform.position = transform.position;
        _cometBody.velocity = rb2d.velocity / 3;
        _cometValid = true;

        hero.GetComponent<MeshRenderer>().enabled = false;
        hero.RelinquishControl();

        var up = InputHandler.Instance.inputActions.up;
        var down = InputHandler.Instance.inputActions.down;
        var left = InputHandler.Instance.inputActions.left;
        var right = InputHandler.Instance.inputActions.right;

        var dashed = false;

        while (_remainingTime > 0)
        {
            if (_cometValid && rb2d.gravityScale == 0)
            {
                _regainControl = false;
                _show = false;
                _cometValid = false;
            }

            if (!_cometValid) break;

            _remainingTime -= Time.deltaTime;

            var target = new Vector2();
            if (up.IsPressed) target.y += 25;
            if (down.IsPressed) target.y -= 25;
            var rightP = right.IsPressed;
            var leftP = left.IsPressed;
            if (rightP != leftP)
            {
                if (rightP)
                {
                    hero.FaceRight();
                    target.x += 25;
                }
                else
                {
                    hero.FaceLeft();
                    target.x -= 25;
                }
            }

            rb2d.velocity = Vector2.zero;
            hero.transform.position = _comet.transform.position + new Vector3(0, 1);
            _cometBody.velocity = Vector2.Lerp(_cometBody.velocity, target, Time.deltaTime * 1.5f);

            if (InputHandler.Instance.inputActions.dash.WasPressed)
            {
                dashed = true;
                hero.SetStartWithDash();
                break;
            }

            yield return null;
        }

        if (_regainControl) hero.RegainControl();
        if (_show) hero.GetComponent<MeshRenderer>().enabled = true;

        if (!dashed) hero.airDashed = false;
        hero.doubleJumped = false;

        _comet.SetActive(false);
        _cometValid = false;

        _source.Stop();
        _source.PlayOneShot(_loseFeather);

        _remainingTime = 0;

        gameObject.BroadcastEvent("OnFinish");
        ResetHitboxes();
    }

    private IEnumerator Respawn()
    {
        for (var i = 0; i <= 5; i++)
        {
            _spriteRenderer.sprite = BreakAnim[i];
            yield return new WaitForSeconds(0.066f);
        }

        yield return new WaitForSeconds(respawnTime);
        _enabled = true;
    }

    private class CometMovement : MonoBehaviour
    {
        private bool _blink;

        private float _spriteChangeTime;

        private void Update()
        {
            if (_remainingTime < 2)
            {
                _spriteChangeTime += Time.deltaTime * 5;
                if (_spriteChangeTime >= 1 * _remainingTime)
                {
                    _spriteChangeTime--;
                    _blink = !_blink;
                    _cometRenderer.sprite = _blink ? CometBlink : Comet;
                    _cometParticleRenderer.material = _blink ? CometBlinkTrail : CometTrail;
                }
            }
        } 
    }

    public static void SetRotation(float rot) => _comet.transform.SetRotation2D(rot);
    
    public static float GetRotation() => _comet.transform.GetRotation2D();
}