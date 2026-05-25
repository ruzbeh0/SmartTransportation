import { Entity } from "cs2/bindings";

export type VehiclePrefabOption = {
    entity: Entity;
    id: string;
    locked: boolean;
    multiunit: boolean;
    requirements: unknown[];
    thumbnail: string;
    objectRequirementIcons: string[] | null;
};

export type TransportVehicleOptions = {
    transportType: string;
    availablePrimaryVehicles: VehiclePrefabOption[];
    availableSecondaryVehicles: VehiclePrefabOption[];
};
