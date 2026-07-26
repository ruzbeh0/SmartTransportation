// index.tsx
import { ModRegistrar } from "cs2/modding";
import { bindValue, useValue } from "cs2/api";
import SmartTransportationMenu from "mods/SmartTransportationMenu";
import "intl";
import "intl/locale-data/jsonp/en-US";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver";
import mod from "../mod.json";

const useUniversalModMenu$ = bindValue<boolean>(mod.id, "useUniversalModMenu", false);

function SmartTransportationTopLeft() {
    const useUniversalModMenu = useValue(useUniversalModMenu$);

    return <>{!useUniversalModMenu && <SmartTransportationMenu />}</>;
}

function SmartTransportationUniversalModMenu() {
    const useUniversalModMenu = useValue(useUniversalModMenu$);

    return <>{useUniversalModMenu && <SmartTransportationMenu />}</>;
}

const register: ModRegistrar = (moduleRegistry) => {
    // Same place as TripsView: top left overlay
    VanillaComponentResolver.setRegistry(moduleRegistry);

    moduleRegistry.append("GameTopLeft", SmartTransportationTopLeft);
    moduleRegistry.append("UniversalModMenu", SmartTransportationUniversalModMenu);
};

export default register;
