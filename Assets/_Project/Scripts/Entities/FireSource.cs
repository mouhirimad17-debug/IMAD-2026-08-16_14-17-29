using System.Collections.Generic;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Marks a prop as a fire source per Part 7.2 ("أي غرض من التصنيف 'ثابت قابل
    /// للسقوط، مصدر نار عند الاشتعال'" - the standing/held candelabra in Part 4.2's
    /// props table). Each instance registers itself so PlayerCarry's wind-ignition
    /// check can scan for nearby fire without every player needing a trigger
    /// collider dance - a plain distance check against Part 7.2's 1m detection radius.
    /// </summary>
    public class FireSource : MonoBehaviour
    {
        public const float DetectionRadius = 1f; // Part 7.2

        public static readonly List<FireSource> Active = new List<FireSource>();

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);
    }
}
