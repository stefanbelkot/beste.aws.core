
import { Observable } from "rxjs";

export class Command{
    CommandName:string;
    CommandData:any;
    constructor(commandName?:string, commandData?:any) {
        this.CommandName = commandName;
        this.CommandData = commandData;
    }
}

export class Page{
    Title:string; 
    Component: any;
    Visible: boolean;
    SubPagesOpen: boolean;
    SubPages: Array<Page>;

}
//#region "Users"
export class BesteUserAuthentificationResponse
{
    Result:BesteUserAuthentificationResult;
    UserData:User;
}
export enum BesteUserAuthentificationResult {

    USER_UNKNOWN = "USER_UNKNOWN",
    SUCCESS = "SUCCESS",
    WRONG_PASSWORD = "WRONG_PASSWORD",
    WRONG_PASSWORD_COUNTER_TOO_HIGH = "WRONG_PASSWORD_COUNTER_TOO_HIGH",
    WRONG_PARAMETER = "WRONG_PARAMETER",
    MUST_CHANGE_PASSWORT = "MUST_CHANGE_PASSWORT",
    JSON_ERROR = "JSON_ERROR",
    RIGHT_VIOLATION = "RIGHT_VIOLATION",
    UNKNOWN_EXCEPTION = "UNKNOWN_EXCEPTION"    
}
export class User
{
    UserId:number = 0;
    Firstname:string = "";
    Lastname:string = "";
    Email:string = "";
    Username:string = "";
    Password:string = "";
    SaltValue:number= 0;
    MustChangePassword:boolean = false;
    WrongPasswordCounter:number = 0;
    open:boolean = false;
}
export class HasRightsResponse
{
    Result:HasRightsResult;
    Rights:Array<Right>;
}
export enum HasRightsResult {
    RIGHT_VIOLATION = "RIGHT_VIOLATION",
    EXCEPTION = "EXCEPTION",
    SUCCESS = "SUCCESS"
}

export class Right
{
    Action:string = "";
    Ressource:string = "";
    RessourceId:number= null;
    HasRight:boolean = false;
    constructor(action:string, ressource:string, ressourceId:number) {
        this.Action = action;
        this.Ressource = ressource;
        this.RessourceId = ressourceId;
    }
}

export class GetUsersResponse
{
    Result:GetUsersResult;
    Users:Array<User>;
}
export enum GetUsersResult {
    SUCCESS = "SUCCESS",
    EXCEPTION = "EXCEPTION",
    RIGHT_VIOLATION = "RIGHT_VIOLATION",
    JSON_ERROR = "JSON_ERROR",
    USER_UNKNOWN = "USER_UNKNOWN",
    TOO_MANY_RESULTS = "TOO_MANY_RESULTS"
}
export class GetUsersParams
{
    Limit:number;
    Offset:number;
    SortUsersBy:SortUsersBy;
}
export enum SortUsersBy {
    USERNAME = "USERNAME",
    EMAIL = "EMAIL",
    LASTNAME = "LASTNAME",
    ID = "ID"
}

export class ModifyUserResponse
{
    Result:ModifyUserResult;
    MandatoryUserParams:MandatoryUserParams;
    PasswordRules:PasswordRules;
    UserData:User;
}
export enum ModifyUserResult {
    MISSING_USER_PARAMS = "MISSING_USER_PARAMS",
    PASSWORD_GUIDELINES_ERROR = "PASSWORD_GUIDELINES_ERROR",
    SUCCESS = "SUCCESS",
    UNKNOWN_EXCEPTION = "UNKNOWN_EXCEPTION",
    USER_UNKNOWN = "USER_UNKNOWN",
    WRONG_PARAMETER = "WRONG_PARAMETER",
    USER_ALREADY_EXISTS = "USER_ALREADY_EXISTS",
    RIGHT_VIOLATION = "RIGHT_VIOLATION",
    JSON_ERROR = "JSON_ERROR"
}

export class MandatoryUserParams
{
    Firstname:boolean;
    Lastname:boolean;
    EMail:boolean;
}
export class PasswordRules
{
    MinLength:number;
    HasDigit:boolean;
    HasLowerCase:boolean;
    HasUpperCase:boolean;
    HasSpecialChars:boolean;
}
//#endregion

//#region "Server"
export class ServerSetting{
    Id:number;
    User:User;
    ServerName:string = "My Game Host";
    ServerDescription:string = "A 7 Days to Die server";
    ServerPassword:string = "BestePassword";
    GameWorld:string = "RWG";
    WorldGenSeed:string = "BesteSoftwareSeed";
    GameName:string = "My Game";
    IsRunning:boolean = false;
    open:boolean = false;
    openServerLog:boolean = false;
}

export class ModifySettingsResponse
{
    Result:ModifySettingsResult;
}
export enum ModifySettingsResult {
    RIGHT_VIOLATION = "RIGHT_VIOLATION",
    GAME_SEED_ALREADY_EXISTS = "GAME_SEED_ALREADY_EXISTS",
    SETTING_ADDED = "SETTING_ADDED",
    USER_NOT_FOUND = "USER_NOT_FOUND",
    EXCEPTION = "EXCEPTION",
    SETTING_NOT_FOUND = "SETTING_NOT_FOUND",
    SETTING_EDITED = "SETTING_EDITED",
    SETTING_DELETED = "SETTING_DELETED"
}

export class StartServerResponse
{
    Result:StartServerResult;
}
export enum StartServerResult {
    RIGHT_VIOLATION = "RIGHT_VIOLATION",
    RIGHT_VIOLATION_SERVERSETTING = "RIGHT_VIOLATION_SERVERSETTING",
    SERVER_STARTED = "SERVER_STARTED",
    SERVER_ALREADY_RUNNING = "SERVER_ALREADY_RUNNING",
    SERVER_COUNT_OF_USER_EXCEEDING = "SERVER_COUNT_OF_USER_EXCEEDING",
    SETTING_NOT_FOUND = "SETTING_NOT_FOUND",
    UNKNOWN_SETTINGS_RESPONSE = "UNKNOWN_SETTINGS_RESPONSE",
    EXCEPTION = "EXCEPTION",
    NO_FREE_PORT = "NO_FREE_PORT"
}
export class StopServerResponse
{
    Result:StopServerResult;
}
export enum StopServerResult {
    RIGHT_VIOLATION = "RIGHT_VIOLATION",
    SERVER_STOPPED = "SERVER_STOPPED",
    SERVER_KILLED = "SERVER_KILLED",
    EXCEPTION = "EXCEPTION",
    FAILED_UNKNOWN_REASON = "FAILED_UNKNOWN_REASON",
    STOPPING = "STOPPING"
}
export class ConnectTelnetResponse
{
    Result:ConnectTelnetResult;
}
export enum ConnectTelnetResult {
    RIGHT_VIOLATION = "RIGHT_VIOLATION",
    SERVER_NOT_RUNNING = "SERVER_NOT_RUNNING",
    EXCEPTION = "EXCEPTION",
    OK = "OK"
}
//#endregion