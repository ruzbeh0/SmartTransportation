// src/AddCustomRulePanel.tsx
import React, { useEffect, useMemo, useState } from "react";
import { trigger, useValue } from "cs2/api";
import { Color, Entity } from "cs2/bindings";
import { TextInput } from "../components/TextInput";
import { Button, DraggablePanelProps, Dropdown, DropdownItem, DropdownToggle, Icon, Panel, Scrollable, Tooltip } from "cs2/ui";
import { useLocalization } from "cs2/l10n";
import styles from "mods/AddCustomRuleComponent/AddCustomRulePanel.module.scss";
import IntInput from "../components/IntInput";
import { CustomRule } from "mods/Domain/customRule";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver";
import { transportVehicleOptionsBinding$ } from "mods/bindings";
import { TransportVehicleOptions, VehiclePrefabOption } from "mods/Domain/transportVehicleOptions";
import { ModuleResolver } from "mods/moduleResolver";

const defaultRouteColor: Color = { r: 0.0, g: 0.54, b: 0.85, a: 1.0 };
const notSpecifiedTransportType = "NotSpecified";
const transportTypeOptions = [
    notSpecifiedTransportType,
    "Bus",
    "Tram",
    "Subway",
    "Train",
    "Ship",
    "Airplane",
    "Ferry",
];

const builtInRuleNames = new Set([
    "Bus",
    "Tram",
    "Train",
    "Subway",
    "Ship",
    "Airplane",
    "Ferry",
]);

const formatTransportType = (transportType: string) =>
    transportType === notSpecifiedTransportType ? "Not Specified" : transportType;

const getInitialTransportType = (rule?: CustomRule | null) => {
    if (!rule) {
        return notSpecifiedTransportType;
    }

    if (rule.transportType && rule.transportType !== notSpecifiedTransportType) {
        return rule.transportType;
    }

    return builtInRuleNames.has(rule.ruleName) ? rule.ruleName : notSpecifiedTransportType;
};

const entityKey = (entity: Entity | null | undefined) =>
    entity ? `${entity.index}:${entity.version}` : "";

interface AddCustomRulePanelProps {
    onRuleSaved: () => void;
    onClose: () => void;
    initialRule?: CustomRule | null;
}

interface VehicleSelectorProps {
    title: string;
    vehicles: VehiclePrefabOption[];
    selectedVehicles: Entity[];
    onToggle: (vehicle: Entity) => void;
}

interface OptionToggleProps {
    label: string;
    selected: boolean;
    onToggle: (selected: boolean) => void;
    disabled?: boolean;
}

const OptionToggle: React.FC<OptionToggleProps> = ({
    label,
    selected,
    onToggle,
    disabled = false,
}) => (
    <Button
        variant="flat"
        selected={selected}
        disabled={disabled}
        className={`${styles.optionToggle} ${selected ? styles.optionToggleSelected : ""}`}
        onSelect={() => !disabled && onToggle(!selected)}
    >
        <span className={`${styles.optionToggleBox} ${selected ? styles.optionToggleBoxSelected : ""}`} />
        <span className={styles.optionToggleLabel}>{label}</span>
    </Button>
);

const VehicleSelector: React.FC<VehicleSelectorProps> = ({
    title,
    vehicles,
    selectedVehicles,
    onToggle,
}) => {
    const { translate } = useLocalization();

    if (!vehicles.length) {
        return null;
    }

    const selectedKeys = new Set(selectedVehicles.map(entityKey));

    return (
        <div className={styles.vehicleSelectorSection}>
            <div className={styles.labelStyle}>{title}</div>
            <Scrollable
                vertical
                trackVisibility="scrollable"
                className={styles.vehicleGridScroll}
            >
                <div className={styles.vehicleGrid}>
                    {vehicles.map((vehicle) => {
                        const selected = selectedKeys.has(entityKey(vehicle.entity));
                        const displayName = translate(`Assets.NAME[${vehicle.id}]`, vehicle.id) ?? vehicle.id;

                        return (
                            <Tooltip
                                key={`${vehicle.id}-${entityKey(vehicle.entity)}`}
                                tooltip={<ModuleResolver.instance.FormattedParagraphs children={displayName} />}
                            >
                                <Button
                                    variant="flat"
                                    selected={selected}
                                    disabled={vehicle.locked}
                                    className={`${styles.vehicleButton} ${selected ? styles.vehicleButtonSelected : ""}`}
                                    onSelect={() => onToggle(vehicle.entity)}
                                >
                                    <Icon src={vehicle.thumbnail} className={styles.vehicleThumbnail} />
                                    <span className={styles.vehicleName}>{displayName}</span>
                                </Button>
                            </Tooltip>
                        );
                    })}
                </div>
            </Scrollable>
        </div>
    );
};

const AddCustomRulePanel: React.FC<AddCustomRulePanelProps & DraggablePanelProps> = ({
    onClose,
    onRuleSaved,
    initialRule,
}) => {
    const isEditing = !!initialRule?.ruleId;
    const isBuiltInRule = !!initialRule && builtInRuleNames.has(initialRule.ruleName);
    const ColorField = VanillaComponentResolver.instance.ColorField;
    const vehicleOptions = useValue(transportVehicleOptionsBinding$);

    const [ruleName, setRuleName] = useState(initialRule?.ruleName ?? "");
    const [occupancy, setOccupancy] = useState<number>(initialRule?.occupancy ?? 80);
    const [stdTicket, setStdTicket] = useState<number>(initialRule?.stdTicket ?? 10);
    const [maxTicketInc, setMaxTicketInc] = useState<number>(initialRule?.maxTicketInc ?? 20);
    const [maxTicketDec, setMaxTicketDec] = useState<number>(initialRule?.maxTicketDec ?? 20);
    const [maxVehAdj, setMaxVehAdj] = useState<number>(initialRule?.maxVehAdj ?? 30);
    const [minVehAdj, setMinVehAdj] = useState<number>(initialRule?.minVehAdj ?? 30);
    const [routeColor, setRouteColor] = useState<Color>(initialRule?.routeColor ?? defaultRouteColor);
    const [useRouteColor, setUseRouteColor] = useState<boolean>(initialRule?.useRouteColor ?? false);
    const [transportType, setTransportType] = useState(getInitialTransportType(initialRule));
    const [useVehicleModels, setUseVehicleModels] = useState<boolean>(initialRule?.useVehicleModels ?? false);
    const [selectedPrimaryVehicles, setSelectedPrimaryVehicles] = useState<Entity[]>(initialRule?.selectedPrimaryVehicles ?? []);
    const [selectedSecondaryVehicles, setSelectedSecondaryVehicles] = useState<Entity[]>(initialRule?.selectedSecondaryVehicles ?? []);

    const selectedVehicleOptions: TransportVehicleOptions | undefined = useMemo(
        () => vehicleOptions.find((option) => option.transportType === transportType),
        [vehicleOptions, transportType]
    );

    const toggleVehicle = (
        vehicle: Entity,
        selectedVehicles: Entity[],
        setSelectedVehicles: React.Dispatch<React.SetStateAction<Entity[]>>
    ) => {
        const key = entityKey(vehicle);
        const isSelected = selectedVehicles.some((selected) => entityKey(selected) === key);
        setSelectedVehicles(
            isSelected
                ? selectedVehicles.filter((selected) => entityKey(selected) !== key)
                : [...selectedVehicles, vehicle]
        );
    };

    useEffect(() => {
        if (!initialRule) {
            setRuleName("");
            setOccupancy(80);
            setStdTicket(10);
            setMaxTicketInc(20);
            setMaxTicketDec(20);
            setMaxVehAdj(30);
            setMinVehAdj(30);
            setRouteColor(defaultRouteColor);
            setUseRouteColor(false);
            setTransportType(notSpecifiedTransportType);
            setUseVehicleModels(false);
            setSelectedPrimaryVehicles([]);
            setSelectedSecondaryVehicles([]);
            return;
        }

        setRuleName(initialRule.ruleName);
        setOccupancy(initialRule.occupancy);
        setStdTicket(initialRule.stdTicket);
        setMaxTicketInc(initialRule.maxTicketInc);
        setMaxTicketDec(initialRule.maxTicketDec);
        setMaxVehAdj(initialRule.maxVehAdj);
        setMinVehAdj(initialRule.minVehAdj);
        setRouteColor(initialRule.routeColor ?? defaultRouteColor);
        setUseRouteColor(initialRule.useRouteColor ?? false);
        setTransportType(getInitialTransportType(initialRule));
        setUseVehicleModels(initialRule.useVehicleModels ?? false);
        setSelectedPrimaryVehicles(initialRule.selectedPrimaryVehicles ?? []);
        setSelectedSecondaryVehicles(initialRule.selectedSecondaryVehicles ?? []);
    }, [initialRule]);

    useEffect(() => {
        if (transportType === notSpecifiedTransportType) {
            setUseVehicleModels(false);
            setSelectedPrimaryVehicles([]);
            setSelectedSecondaryVehicles([]);
            return;
        }

        if (!selectedVehicleOptions) {
            return;
        }

        const availablePrimaryKeys = new Set(
            selectedVehicleOptions.availablePrimaryVehicles.map((vehicle) => entityKey(vehicle.entity))
        );
        const availableSecondaryKeys = new Set(
            selectedVehicleOptions.availableSecondaryVehicles.map((vehicle) => entityKey(vehicle.entity))
        );

        setSelectedPrimaryVehicles((vehicles) =>
            vehicles.filter((vehicle) => availablePrimaryKeys.has(entityKey(vehicle)))
        );
        setSelectedSecondaryVehicles((vehicles) =>
            vehicles.filter((vehicle) => availableSecondaryKeys.has(entityKey(vehicle)))
        );
    }, [transportType, selectedVehicleOptions]);

    const saveRule = () => {
        const savedTransportType = isBuiltInRule
            ? getInitialTransportType(initialRule)
            : transportType;
        const savedUseVehicleModels = useVehicleModels && savedTransportType !== notSpecifiedTransportType;

        const payload = {
            ...(isEditing ? { ruleId: initialRule!.ruleId } : {}),
            ruleName: isBuiltInRule ? initialRule!.ruleName : ruleName,
            occupancy,
            stdTicket,
            maxTicketInc,
            maxTicketDec,
            maxVehAdj,
            minVehAdj,
            routeColor,
            useRouteColor,
            transportType: savedTransportType,
            useVehicleModels: savedUseVehicleModels,
            selectedPrimaryVehicles: savedUseVehicleModels ? selectedPrimaryVehicles : [],
            selectedSecondaryVehicles: savedUseVehicleModels ? selectedSecondaryVehicles : [],
        };

        trigger(
            "smartTransportation",
            isEditing ? "editCustomRule" : "addCustomRule",
            payload
        );

        onRuleSaved();
        onClose();
    };

    return (
        <Panel
            draggable={true}
            onClose={onClose}
            initialPosition={{
                x: 0.38,
                y: 0.5
            }}
            className={styles.panel}
            header={
            <div className={styles.header}>
              <span className={styles.headerText}>
                  {isBuiltInRule
                      ? "Smart Transportation - Edit Default Rule"
                      : isEditing
                      ? "Smart Transportation - Edit Custom Rule"
                      : "Smart Transportation - Add Custom Rule"}
              </span>
            </div>
          }
            
        >
            <div className={styles.formLayout}>
                <Scrollable
                    vertical
                    trackVisibility="scrollable"
                    className={styles.formScroll}
                >
                    <div className={styles.formContent}>
                        <div>
                            <div className={styles.labelStyle}>
                                Rule name
                            </div>
                            {isBuiltInRule ? (
                                <div className={styles.readOnlyValue}>
                                    {ruleName}
                                </div>
                            ) : (
                                <TextInput
                                    id="ruleName"
                                    value={ruleName}
                                    onChange={setRuleName}
                                    placeholder="Enter rule name. . ."
                                />
                            )}
                        </div>

                        <div>
                            <OptionToggle
                                label="Select Color"
                                selected={useRouteColor}
                                onToggle={setUseRouteColor}
                            />
                            {useRouteColor && (
                                <div>
                                    <div className={styles.labelStyle}>
                                        Route color
                                    </div>
                                    <div className={styles.colorPickerRow}>
                                        <ColorField
                                            value={routeColor}
                                            alpha={false}
                                            className={styles.colorField}
                                            onChange={setRouteColor}
                                        />
                                    </div>
                                </div>
                            )}

                    <div>
                        <div className={styles.labelStyle}>
                            Transport type
                        </div>
                        {isBuiltInRule ? (
                            <div className={styles.readOnlyValue}>
                                {formatTransportType(transportType)}
                            </div>
                        ) : (
                            <Dropdown
                                content={transportTypeOptions.map((option) => (
                                    <DropdownItem
                                        key={option}
                                        value={option}
                                        selected={transportType === option}
                                        onChange={() => setTransportType(option)}
                                    >
                                        {formatTransportType(option)}
                                    </DropdownItem>
                                ))}
                            >
                                <DropdownToggle>{formatTransportType(transportType)}</DropdownToggle>
                            </Dropdown>
                        )}
                    </div>

                    {transportType !== notSpecifiedTransportType && (
                        <OptionToggle
                            label="Select Default Vehicles"
                            selected={useVehicleModels}
                            onToggle={setUseVehicleModels}
                        />
                    )}

                    {useVehicleModels && transportType !== notSpecifiedTransportType && selectedVehicleOptions && (
                        <div className={styles.vehicleSelectors}>
                            <VehicleSelector
                                title="Default vehicles"
                                vehicles={selectedVehicleOptions.availablePrimaryVehicles}
                                selectedVehicles={selectedPrimaryVehicles}
                                onToggle={(vehicle) => toggleVehicle(vehicle, selectedPrimaryVehicles, setSelectedPrimaryVehicles)}
                            />
                            <VehicleSelector
                                title="Carriages"
                                vehicles={selectedVehicleOptions.availableSecondaryVehicles}
                                selectedVehicles={selectedSecondaryVehicles}
                                onToggle={(vehicle) => toggleVehicle(vehicle, selectedSecondaryVehicles, setSelectedSecondaryVehicles)}
                            />
                        </div>
                    )}

                    <div>
                        <div className={styles.labelStyle}>
                            Occupancy target (%)
                        </div>
                        <IntInput
                            id="occupancy"
                            value={occupancy}
                            onChange={setOccupancy}
                            placeholder= {80}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Standard ticket
                        </div>
                        <IntInput
                            id="stdTicket"
                            value={stdTicket}
                            onChange={setStdTicket}
                            placeholder={10}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Maximum ticket increase (%)
                        </div>
                        <IntInput
                            id="maxTicketInc"
                            value={maxTicketInc}
                            onChange={setMaxTicketInc}
                            placeholder={20}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Maximum ticket decrease (%)
                        </div>
                        <IntInput
                            id="maxTicketDec"
                            value={maxTicketDec}
                            onChange={setMaxTicketDec}
                            placeholder={20}
                        />
                    </div>

                    <div>
                        <div className={styles.labelStyle}>
                            Minimum vehicle adjustment (%)
                        </div>
                        <IntInput
                            id="minVehAdj"
                            value={minVehAdj}
                            onChange={setMinVehAdj}
                            placeholder={30}
                        />
                    </div>

                            <div>
                                <div className={styles.labelStyle}>
                                    Maximum vehicle adjustment (%)
                                </div>
                                <IntInput
                                    id="maxVehAdj"
                                    value={maxVehAdj}
                                    onChange={setMaxVehAdj}
                                    placeholder={30}
                                />
                            </div>
                        </div>
                    </div>
                </Scrollable>

                <div className={styles.buttonSection}>
                    <Button
                        variant="flat"
                        className={styles.buttonStyle}
                        onSelect={onClose}
                    >
                        Cancel
                    </Button>
                    <Button
                        variant="flat"
                        className={styles.buttonStyle}
                        onSelect={saveRule}
                        >
                        {isEditing ? "Save Changes" : "Save"}
                    </Button>
                </div>
                
            </div>
        </Panel>
    );
};

export default AddCustomRulePanel;
