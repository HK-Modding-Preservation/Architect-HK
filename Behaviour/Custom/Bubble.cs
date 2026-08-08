using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Architect.Behaviour.Custom;

public class Bubble : SoundMaker, IHitResponder
{
    private const string BUBBLE_SOURCE = "BubbleMoving";

    public static readonly BubbleAnims Red = new("Red");

    public float speed = 24;
    public float respawnTime = 5;
    
    private static AudioClip _entry;
    private static AudioClip _loop;
    private static AudioClip _end;
    private static AudioClip _reappear;
    
    public static void Init()
    {
        ModHooks.TakeDamageHook += (ref _, damage) =>
        {
            if (_current)
            {
                _current.Pop();
                _current = null;
            }
            return damage;
        };
        
        ResourceUtils.LoadClipResource("Bubble.entry", clip => _entry = clip);
        ResourceUtils.LoadClipResource("Bubble.loop", clip => _loop = clip);
        ResourceUtils.LoadClipResource("Bubble.end", clip => _end = clip);
        ResourceUtils.LoadClipResource("Bubble.reappear", clip => _reappear = clip);
    }

    private static Sprite[] LoadSprites(string path, int count)
    {
        var sprites = new Sprite[count];
        for (var c = 0; c < count; c++) 
            sprites[c] = ResourceUtils.LoadSpriteResource($"{path}.f{c}", FilterMode.Point, ppu: 10);
        return sprites;
    }

    public BubbleStage stage;
    private float _frame;
    private Vector2 _startPos;
    
    private SpriteRenderer _renderer;
    private ParticleSystem _ps;
    private ParticleSystem.EmissionModule _emission;
    private CircleCollider2D _col2d;

    public override void Awake()
    {
        base.Awake();
        _renderer = GetComponent<SpriteRenderer>();
        _ps = GetComponentInChildren<ParticleSystem>();
        _emission = _ps.emission;
        _col2d = GetComponent<CircleCollider2D>();
        
        transform.SetPositionZ(transform.GetPositionZ() + Random.value / 100f);
    }

    private static AudioSource _source;
    
    private void Start()
    {
        var sounds = HeroController.instance.transform.Find("Sounds");

        _source = sounds.Find(BUBBLE_SOURCE)?.GetComponent<AudioSource>();
        if (!_source)
        {
            _source = new GameObject(BUBBLE_SOURCE)
            {
                transform = { parent = sounds }
            }.AddComponent<AudioSource>();
            _source.loop = true;
        }
    }
    
    public void Hit(HitInstance damageInstance)
    {
        if (damageInstance.AttackType != AttackTypes.Nail) return;
        
        _col2d.enabled = false;
        _startPos = transform.position;
        Pop();
    }

    private static Bubble _current;

    private IEnumerator OnDetect(HeroController hero)
    {
        gameObject.BroadcastEvent("OnActivate");
        
        _col2d.enabled = false;
        _frame = 0;
        stage = BubbleStage.Start;
        _startPos = transform.position;
        _current = this;
        
        PlaySound(_entry, 1, 1, true);
        
        hero.RelinquishControl();
        hero.rb2d.constraints = RigidbodyConstraints2D.FreezeAll;
        hero.renderer.enabled = false;

        var ia = hero.inputHandler.inputActions;
        
        var currentSpeed = Vector3.zero;
        var begunSpin = false;

        hero.SetHeroParent(transform);
        hero.transform.localPosition = new Vector3(0, 0.725f, 0);
        while (_current == this)
        {
            if (stage == BubbleStage.Spin)
            {
                if (!begunSpin)
                {
                    if (ia.left.IsPressed) currentSpeed.x -= 1;
                    if (ia.right.IsPressed) currentSpeed.x += 1;
                    if (ia.up.IsPressed) currentSpeed.y += 1;
                    if (ia.down.IsPressed) currentSpeed.y -= 1;

                    if (currentSpeed == Vector3.zero) currentSpeed.x += hero.cState.facingRight ? 1 : -1;
                    
                    currentSpeed = currentSpeed.normalized * speed;

                    begunSpin = true;

                    _source.clip = _loop;
                    _source.volume = GameManager.instance.GetImplicitCinematicVolume() / 3;
                    _source.Play();
                }
                
                transform.position += currentSpeed * Time.deltaTime;
            }

            if (ia.dash.WasPressed)
            {
                hero.startWithDash = true;
                _current = null;
                break;
            }
            
            yield return null;
        }

        if (!_current)
        {
            hero.EnableRenderer();
            hero.rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
            hero.RegainControl();
            hero.airDashed = false;
            hero.doubleJumped = false;
            hero.SetHeroParent(null);

            _source.Stop();
        }

        Pop();
    }

    private float _respawnTime;

    private void Update()
    {
        if (_respawnTime > 0)
        {
            _respawnTime -= Time.deltaTime;
            if (_respawnTime > 0) return;
            _renderer.enabled = true;
        }

        var sprites = Red.GetSprites(stage);
        _frame += Time.deltaTime * 12;

        if (_frame >= sprites.Length)
        {
            switch (stage)
            {
                case BubbleStage.Start:
                    stage = BubbleStage.Spin;
                    _emission.rateOverTimeMultiplier = 25;
                    break;
                case BubbleStage.Pop:
                    transform.SetPosition2D(_startPos);
                    PlaySound(_reappear);
                    stage = BubbleStage.Appear;
                    _respawnTime = respawnTime;
                    if (_respawnTime > 0) _renderer.enabled = false;
                    break;
                case BubbleStage.Appear:
                    stage = BubbleStage.Idle;
                    _col2d.enabled = true;
                    break;
            }

            _frame = 0;
            sprites = Red.GetSprites(stage);
        }
        
        _renderer.sprite = sprites[Mathf.FloorToInt(_frame)];
    }

    private void Pop()
    {
        _frame = 0;
        stage = BubbleStage.Pop;
        PlaySound(_end, 1, 1, true);
        _emission.rateOverTimeMultiplier = 0;
    }

    public class Detector : MonoBehaviour
    {
        public Bubble bubble;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (bubble.stage != BubbleStage.Idle) return;
            
            var hero = other.GetComponent<HeroController>();
            if (!hero) return;
            
            if (hero.controlReqlinquished)
            {
                if (_current) _current.Pop();
                else return;
            }
            
            bubble.StartCoroutine(bubble.OnDetect(hero));
        }
    }

    public class TerrainDetector : MonoBehaviour
    {
        public Bubble bubble;

        private void OnCollisionStay2D(Collision2D _)
        {
            if (bubble.stage != BubbleStage.Spin) return;
            if (_current == bubble) _current = null;
            bubble.Pop();
        }
    }

    public class BubbleAnims(string name)
    {
        public readonly Sprite[] Idle = LoadSprites($"Bubble.{name}.Idle", 5);
        private readonly Sprite[] _start = LoadSprites($"Bubble.{name}.Start", 4);
        private readonly Sprite[] _spin = LoadSprites($"Bubble.{name}.Spin", 8);
        private readonly Sprite[] _pop = LoadSprites($"Bubble.{name}.Pop", 5);
        private readonly Sprite[] _appear = LoadSprites($"Bubble.{name}.Appear", 5);

        public Sprite[] GetSprites(BubbleStage stage)
        {
            return stage switch
            {
                BubbleStage.Idle => Idle,
                BubbleStage.Start => _start,
                BubbleStage.Spin => _spin,
                BubbleStage.Pop => _pop,
                BubbleStage.Appear => _appear,
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
            };
        }
    }

    public enum BubbleStage
    {
        Idle,
        Start,
        Spin,
        Pop,
        Appear
    }
}