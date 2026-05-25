import { Color, Entity } from "cs2/bindings";

export type AddCustomRule = {
    ruleName: string;
    occupancy: number;
    stdTicket: number;
    maxTicketInc: number;
    maxTicketDec: number;
    maxVehAdj: number;
    minVehAdj: number;
    routeColor: Color;
    useRouteColor: boolean;
    transportType: string;
    useVehicleModels: boolean;
    selectedPrimaryVehicles: Entity[];
    selectedSecondaryVehicles: Entity[];
}
