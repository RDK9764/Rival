using System;
using System.Collections.Generic;
using UnityEngine;
using Rival.Core;
using Rival.Utilities;

namespace Rival.Core
{
    public enum VfxType
    {
        LightHit,
        HeavyHit,
        BlockSpark,
        DeathBurst,
        DashTrail,
        ParryFlash,
        GuardBreak
    }

    /// <summary>
    /// Pooled particle system service. Pre-instantiates prefabs on Awake and
    /// rents/returns them by VfxType. No per-frame allocation. Registered as
    /// IVfxService on the ServiceLocator.
    /// </summary>
    public interface IVfxService
    {
        void Spawn(VfxType type, Vector3 position, Vector3 normal);
        void ReturnAll();
    }

    public class VfxService : MonoBehaviour, IVfxService
    {
        [Serializable]
        private struct VfxEntry
        {
            public VfxType type;
            public GameObject prefab;
            public int poolSize;
        }

        [SerializeField] private VfxEntry[] entries;

        private readonly Dictionary<VfxType, Queue<ParticleSystem>> pools = new();
        private readonly List<ParticleSystem> active = new();

        private void Awake()
        {
            ServiceLocator.Register<IVfxService>(this);
            InitializePools();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<IVfxService>(this);
        }

        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (!active[i].isPlaying)
                {
                    active[i].gameObject.SetActive(false);
                    var type = GetVfxType(active[i]);
                    if (pools.TryGetValue(type, out var queue))
                        queue.Enqueue(active[i]);
                    active.RemoveAt(i);
                }
            }
        }

        public void Spawn(VfxType type, Vector3 position, Vector3 normal)
        {
            if (!pools.TryGetValue(type, out var queue) || queue.Count == 0) return;
            var ps = queue.Dequeue();
            ps.transform.position = position;
            if (normal.sqrMagnitude > 0.01f)
                ps.transform.rotation = Quaternion.LookRotation(normal);
            ps.gameObject.SetActive(true);
            ps.Play();
            active.Add(ps);
        }

        public void ReturnAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                active[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                active[i].gameObject.SetActive(false);
                var type = GetVfxType(active[i]);
                if (pools.TryGetValue(type, out var queue))
                    queue.Enqueue(active[i]);
            }
            active.Clear();
        }

        private void InitializePools()
        {
            foreach (var entry in entries)
            {
                var queue = new Queue<ParticleSystem>();
                for (int i = 0; i < entry.poolSize; i++)
                {
                    GameObject go;
                    if (entry.prefab != null)
                    {
                        go = Instantiate(entry.prefab, transform);
                    }
                    else
                    {
                        go = CreateFallbackParticle(entry.type);
                        go.transform.SetParent(transform);
                    }
                    go.name = $"VFX_{entry.type}_{i}";
                    go.SetActive(false);
                    var ps = go.GetComponent<ParticleSystem>();
                    if (ps != null) queue.Enqueue(ps);
                }
                pools[entry.type] = queue;
            }
        }

        private static GameObject CreateFallbackParticle(VfxType type)
        {
            var go = new GameObject();
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.maxParticles = 20;
            main.loop = false;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(GetVfxColor(type), 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

            return go;
        }

        private static Color GetVfxColor(VfxType type)
        {
            return type switch
            {
                VfxType.LightHit => new Color(1f, 0.8f, 0.3f),
                VfxType.HeavyHit => new Color(1f, 0.4f, 0.1f),
                VfxType.BlockSpark => new Color(0.4f, 0.7f, 1f),
                VfxType.ParryFlash => new Color(1f, 1f, 0.5f),
                VfxType.GuardBreak => new Color(1f, 0.2f, 0.2f),
                VfxType.DeathBurst => new Color(0.8f, 0.1f, 0.1f),
                VfxType.DashTrail => new Color(0.5f, 0.8f, 1f),
                _ => Color.white
            };
        }

        private static VfxType GetVfxType(ParticleSystem ps)
        {
            if (ps == null) return VfxType.LightHit;
            // Name convention: VFX_{type}_{index} — parse type
            var name = ps.gameObject.name;
            foreach (VfxType t in Enum.GetValues(typeof(VfxType)))
            {
                if (name.Contains(t.ToString())) return t;
            }
            return VfxType.LightHit;
        }
    }
}
