// src/AddCustomRulePanel.tsx
import React, { useEffect, useState } from "react";
import { trigger } from "cs2/api";
import { Color } from "cs2/bindings";
import { TextInput } from "../components/TextInput";
import { Button, DraggablePanelProps, Panel } from "cs2/ui";
import styles from "mods/AddCustomRuleComponent/AddCustomRulePanel.module.scss";
import IntInput from "../components/IntInput";
import { CustomRule } from "mods/Domain/customRule";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver";

const defaultRouteColor: Color = { r: 0.0, g: 0.54, b: 0.85, a: 1.0 };

interface AddCustomRulePanelProps {
    onRuleSaved: () => void;
    onClose: () => void;
    initialRule?: CustomRule | null;
}

const AddCustomRulePanel: React.FC<AddCustomRulePanelProps & DraggablePanelProps> = ({
    onClose,
    onRuleSaved,
    initialRule,
}) => {
    const isEditing = !!initialRule?.ruleId;
    const ColorField = VanillaComponentResolver.instance.ColorField;

    const [ruleName, setRuleName] = useState(initialRule?.ruleName ?? "");
    const [occupancy, setOccupancy] = useState<number>(initialRule?.occupancy ?? 80);
    const [stdTicket, setStdTicket] = useState<number>(initialRule?.stdTicket ?? 10);
    const [maxTicketInc, setMaxTicketInc] = useState<number>(initialRule?.maxTicketInc ?? 20);
    const [maxTicketDec, setMaxTicketDec] = useState<number>(initialRule?.maxTicketDec ?? 20);
    const [maxVehAdj, setMaxVehAdj] = useState<number>(initialRule?.maxVehAdj ?? 30);
    const [minVehAdj, setMinVehAdj] = useState<number>(initialRule?.minVehAdj ?? 30);
    const [routeColor, setRouteColor] = useState<Color>(initialRule?.routeColor ?? defaultRouteColor);

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
    }, [initialRule]);

    const saveRule = () => {
        const payload = {
            ...(isEditing ? { ruleId: initialRule!.ruleId } : {}),
            ruleName,
            occupancy,
            stdTicket,
            maxTicketInc,
            maxTicketDec,
            maxVehAdj,
            minVehAdj,
            routeColor,
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
                  {isEditing
                      ? "Smart Transportation - Edit Custom Rule"
                      : "Smart Transportation - Add Custom Rule"}
              </span>
            </div>
          }
            
        >
            <div
                style={{
                    padding: "10px 16px 12px",
                    display: "flex",
                    flexDirection: "column",
                    height: "100%",
                    boxSizing: "border-box",
                }}
            >
                
                <div>
                    <div className={styles.labelStyle}>
                        Rule name
                    </div>
                    <TextInput
                        id="ruleName"
                        value={ruleName}
                        onChange={setRuleName}
                        placeholder="Enter rule name. . ."
                    />
                </div>

                <div>
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
