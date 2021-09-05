using System;
using GuldeLib.Maps;
using MonoExtensions.Runtime;
using MonoLogger.Runtime;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace GuldeLib.Entities
{
    public class EntityComponent : SerializedMonoBehaviour
    {
        [OdinSerialize]
        public Vector3 Position { get; set; }

        [OdinSerialize]
        public LocationComponent Location { get; private set; }

        [OdinSerialize]
        public MapComponent Map { get; private set; }

        public Vector3Int CellPosition => Position.ToCell();

        public event EventHandler<MapEventArgs> MapChanged;
        public event EventHandler<LocationEventArgs> LocationChanged;

        void Awake()
        {
            this.Log("Entity initializing");
        }

        public void SetLocation(LocationComponent location)
        {
            this.Log($"Entity setting location to {location}");

            Location = location;

            LocationChanged?.Invoke(this, new LocationEventArgs(location));
        }

        public void SetMap(MapComponent map)
        {
            this.Log($"Entity setting map to {map}");

            Map = map;

            MapChanged?.Invoke(this, new MapEventArgs(map));
        }

        public void SetCell(Vector3Int cell)
        {
            this.Log($"Entity setting cell to {cell}");

            Position = cell.ToWorld();
        }
    }
}