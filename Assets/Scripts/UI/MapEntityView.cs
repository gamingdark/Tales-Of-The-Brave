using UnityEngine;
using TalesOfTheBrave.Simulation.Movement;
using TalesOfTheBrave.Simulation.World;
using TalesOfTheBrave.Simulation.Entities;

namespace TalesOfTheBrave.Unity.UI
{
    public sealed class MapEntityView : MonoBehaviour
    {
        [Header("Entity")]
        [SerializeField] private string entityId;
        [SerializeField] private string displayName;
        [SerializeField] private string entityType;

        [Header("Live State")]
        [SerializeField] private string state;
        [SerializeField] private string locationNodeId;
        [SerializeField] private string destinationNodeId;
        [SerializeField] private string plannedDestinationNodeId;
        [SerializeField, Range(0f, 1f)] private float edgeProgress;
        [SerializeField, Range(0f, 1f)] private float visualEdgeProgress;

        public string EntityId => entityId;
        public string State => state;
        public string LocationNodeId => locationNodeId;

        public void InitializeEntity(Entity entity)
        {
            entityId = entity.Id;
            displayName = entity.DisplayName;
            entityType = entity.HasBehavior<LocationBehavior>() ? "Location" : "Entity";
            state = "Active";
            locationNodeId = entity.GetBehavior<WorldEntityBehavior>().StartingNodeId;
            name = $"{entityType} - {entity.DisplayName}";
        }

        public void InitializeTransport(Transport transport)
        {
            entityId = transport.Id;
            displayName = transport.DisplayName;
            entityType = "Transport";
            name = $"Ship - {transport.DisplayName}";
        }

        public void RefreshTransport(Transport transport, float dayProgress)
        {
            var travel = transport.Travel;
            locationNodeId = travel.CurrentNodeId;
            destinationNodeId = travel.DestinationNodeId;
            plannedDestinationNodeId = travel.PlannedDestinationNodeId;
            edgeProgress = travel.EdgeProgress;
            visualEdgeProgress = travel.GetVisualEdgeProgress(dayProgress);

            if (travel.IsEnteringLocation(dayProgress)) state = "EnteringLocation";
            else if (travel.IsTravelling) state = "Travelling";
            else if (travel.HasPlannedAction) state = "VoyagePlanned";
            else state = "InPort";
        }
    }
}
