import { Color, Entity } from "cs2/bindings";

export type CustomRule = {
    ruleId: string;
    ruleName: string;
    occupancy: number;
    stdTicket: number;
    maxTicketInc: number;
    maxTicketDec: number;
    maxVehAdj: number;
    minVehAdj: number;
    adjustVehicles: boolean;
    routeColor: Color;
    useRouteColor: boolean;
    transportType: string;
    useVehicleModels: boolean;
    useVehicleColors: boolean;
    vehicleColor0: Color;
    vehicleColor1: Color;
    vehicleColor2: Color;
    useRouteNaming: boolean;
    sequentialRouteNaming: boolean;
    routeNamePrefix: string;
    selectedPrimaryVehicles: Entity[];
    selectedSecondaryVehicles: Entity[];
};
