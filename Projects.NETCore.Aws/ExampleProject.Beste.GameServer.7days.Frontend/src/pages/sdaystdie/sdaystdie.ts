import { Component } from '@angular/core';
import { NavController, NavParams, ModalController, Nav, AlertController, LoadingController, Loading } from 'ionic-angular';
import { WebsocketProvider } from '../../providers/websocket-service';
import { Subject, Subscription } from 'rxjs';
import { Command, User, BesteUserAuthentificationResponse, ServerSetting, ModifySettingsResponse, ModifySettingsResult, StartServerResponse, StartServerResult, StopServerResponse, StopServerResult, ConnectTelnetResponse, ConnectTelnetResult } from '../../app/classes/classes';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { ChangePasswordPage } from '../../modals/change-password/change-password';
import { GlobalVarsSDaysTDie } from '../../providers/globalVarsSDaysTDie';

@Component({
  selector: 'page-sdaystdie',
  templateUrl: 'sdaystdie.html'
})
export class SdaystdiePage {

  inputSignin: FormGroup;
  inputServers: Array<FormGroup> = new Array<FormGroup>();
  serverSettings: Array<ServerSetting> = new Array<ServerSetting>();
  newInputServer: FormGroup;
  newServerSetting: ServerSetting = new ServerSetting();
  serverLogs: Array<String> = new Array<String>();

  socket:Subject<any>;
  impressumVisible:boolean = false;
  loggedIn:boolean = false;
  logoutSubscription:Subscription = null;

  loadingInstance:Loading;

  webSocketProvider:WebsocketProvider = GlobalVarsSDaysTDie.webSocketProvider;
  constructor(public navCtrl: NavController, public navParams: NavParams, public modalCtrl:ModalController,
    private alertCtrl:AlertController, private loadingController: LoadingController) {
      
    this.loggedIn = GlobalVarsSDaysTDie.loggedinUser != null;  
    if(this.loggedIn)
    {
      this.socket = GlobalVarsSDaysTDie.webSocketProvider.getInstance();
      this.getServerSettingsForUser();
      this.generateNewServerSetting();
    }
    this.serverSettings = new Array<ServerSetting>();
  }

  ngOnInit() {
		/* you can compose validators with Validators.compose([ ... ]) */
		this.inputSignin = new FormGroup({
			'username': new FormControl(null, Validators.required),
			'password': new FormControl(null, Validators.required),
			'save': new FormControl(true)
    });
    this.logoutSubscription = null;
    this.logoutSubscription = GlobalVarsSDaysTDie.inactiveTimoutObservable.subscribe(value => {
      GlobalVarsSDaysTDie.loggedinUser = null;
      this.presentConnectionLostAlert();
    });
  }
  presentConnectionLostAlert()
  {
    this.logoutSubscription.unsubscribe();
    let alert = this.alertCtrl.create({
      title: 'Connection Lost',
      cssClass: 'userInactiveAlert',
      subTitle: 'A connection error occured - please log in again!',
      buttons: ['OK']
    });
    alert.present(); 
    this.logout();  
    alert.onDidDismiss(() => 
    {
      this.logoutSubscription = GlobalVarsSDaysTDie.inactiveTimoutObservable.subscribe(value => {
        this.presentConnectionLostAlert();
      });      
    });
  }

  presentLoading(duration:number, content:string) {
    this.loadingInstance = this.loadingController.create({
      content: content,
      duration: duration
    });
    this.loadingInstance.present();
  }
  dismissLoading()
  {
    this.loadingInstance.dismiss();
  }
  ngOnDestroy() {
    this.logoutSubscription.unsubscribe();
  }
  ionViewDidLoad() {
  }

  toggleImpressum() {
    this.impressumVisible = !this.impressumVisible;
  }

//#region "ServerSetting"

  getServerSettingsForUser(){
    this.socket.subscribe(message => {
      //LoggerService.log("Answer: " + message.data);
      let command:Command = new Command();
      command.CommandData = new Array<ServerSetting>();
      command = JSON.parse(message.data);
      if(command.CommandName == "GetServerSettingsResponse"){
        this.generateFormGroupForInputServers(command.CommandData.ServerSettings);
        this.serverSettings = command.CommandData.ServerSettings;
      }
    });
    let command:Command = new Command();
    command.CommandName = "GetServerSettingsOfLoggedInUser";
    command.CommandData = null;
    this.socket.next(command);
  }
  toggleSection(i:number) {
    this.serverSettings[i].open = !this.serverSettings[i].open;
  }
  generateFormGroupForInputServers(serverSettings:Array<ServerSetting>)
  {
    this.inputServers = new Array<FormGroup>();
    this.serverLogs = new Array<string>();        
    for (var i = 0, len = serverSettings.length; i < len; i++) {
      this.inputServers.push(this.generateFormGroupByServerSetting(serverSettings[i]));
      this.serverLogs.push("No Logs available");
    }
  }

  generateFormGroupByServerSetting(serverSetting:ServerSetting) : FormGroup
  {
    let serverSettingGroup:FormGroup = new FormGroup({
			'server_name': new FormControl(null),
			'world_gen_seed': new FormControl(null),
			'server_description': new FormControl(null),
			'server_password': new FormControl(null),
			'game_world': new FormControl(null),
			'game_name': new FormControl(null)    
    });
    serverSettingGroup.setValue({
      'server_name':serverSetting.ServerName,
      'world_gen_seed':serverSetting.WorldGenSeed,
      'server_description':serverSetting.ServerDescription,
      'server_password':serverSetting.ServerPassword,
      'game_world':serverSetting.GameWorld,
      'game_name':serverSetting.GameName
    });
    return serverSettingGroup;
  }
  
  deleteServerSetting(i:number) {
    let alert = this.alertCtrl.create({
      title: 'Delete Server Setting',
      cssClass: 'userInactiveAlert',
      subTitle: "Do you really want to delete the Server: '" + this.serverSettings[i].ServerName + "'?",
      buttons: [
        {
          text: 'Cancel',
          role: 'cancel',
          cssClass: 'secondary',
          handler: () => {
            console.log('Confirm Cancel');
          }
        }, {
          text: 'Ok',
          handler: () => {
            this.socket.subscribe(message => {
              //LoggerService.log("Answer: " + message.data);
              this.callBackToDeleteServerSettings(message);
            });
            this.socket.next(new Command('DeleteServerSettings', this.serverSettings[i]));
          }
        }
      ]
    });
    alert.present();
  }

  private callBackToDeleteServerSettings(message: any) {
    let command: Command = new Command();
    command.CommandData = new ModifySettingsResponse();
    command = JSON.parse(message.data);
    if (command.CommandName == "DeleteServerSettingsResponse") {
      let response: ModifySettingsResponse = command.CommandData;
      if (response.Result == ModifySettingsResult.SETTING_DELETED) {
        let alert = this.alertCtrl.create({
          title: 'Setting deleted',
          cssClass: 'userInactiveAlert',
          subTitle: '',
          buttons: ['OK']
        });
        alert.present();
        this.getServerSettingsForUser();
        this.generateNewServerSetting();
      }
      else
      {
        let alert = this.alertCtrl.create({
          title: 'ERROR: Delete',
          cssClass: 'userInactiveAlert',
          subTitle: response.Result,
          buttons: ['OK']
        });
        alert.present();       
      }
    }
  }

  updateServerSetting(i:number) {
    this.assignFormGroupValuesToServerSettingByIndex(i);
    this.socket.next(new Command('EditServerSettings', this.serverSettings[i]));
    this.socket.subscribe(message => {
      //LoggerService.log("Answer: " + message.data);
      this.callBackToEditServerSettings(message);
    });
  }

  private callBackToEditServerSettings(message: any) {
    let command: Command = new Command();
    command.CommandData = new ModifySettingsResponse();
    command = JSON.parse(message.data);
    if (command.CommandName == "EditServerSettingsResponse") {
      let response: ModifySettingsResponse = command.CommandData;
      if (response.Result == ModifySettingsResult.SETTING_EDITED) {
        let alert = this.alertCtrl.create({
          title: 'Setting updated',
          cssClass: 'userInactiveAlert',
          subTitle: '',
          buttons: ['OK']
        });
        alert.present();
      }
      else
      {
        let alert = this.alertCtrl.create({
          title: 'ERROR: Setting updated',
          cssClass: 'userInactiveAlert',
          subTitle: response.Result,
          buttons: ['OK']
        });
        alert.present();       
      }
    }
  }
  assignFormGroupValuesToServerSettingByIndex(i:number)
  {
    this.serverSettings[i].ServerName = this.inputServers[i].get('server_name').value;
    this.serverSettings[i].WorldGenSeed = this.inputServers[i].get('world_gen_seed').value;
    this.serverSettings[i].ServerDescription = this.inputServers[i].get('server_description').value;
    this.serverSettings[i].ServerPassword = this.inputServers[i].get('server_password').value;
    this.serverSettings[i].GameWorld = this.inputServers[i].get('game_world').value;
    this.serverSettings[i].GameName = this.inputServers[i].get('game_name').value;
  }
  
  toggleNewServerSection() {
    this.newServerSetting.open = !this.newServerSetting.open;
  }
  generateNewServerSetting() {
    this.newServerSetting = new ServerSetting();
    this.newInputServer = this.generateFormGroupByServerSetting(this.newServerSetting);
  }
  addServerSetting() {
    this.assignNewFormGroupValuesToNewServerSetting();
    this.socket.subscribe(message => {
      //LoggerService.log("Answer: " + message.data);
      this.callBackToAddServerSettings(message);
    });
    this.socket.next(new Command('AddServerSetting', this.newServerSetting));
  }
  assignNewFormGroupValuesToNewServerSetting()
  {
    this.newServerSetting.ServerName = this.newInputServer.get('server_name').value;
    this.newServerSetting.WorldGenSeed = this.newInputServer.get('world_gen_seed').value;
    this.newServerSetting.ServerDescription = this.newInputServer.get('server_description').value;
    this.newServerSetting.ServerPassword = this.newInputServer.get('server_password').value;
    this.newServerSetting.GameWorld = this.newInputServer.get('game_world').value;
    this.newServerSetting.GameName = this.newInputServer.get('game_name').value;
  }
  private callBackToAddServerSettings(message: any) {
    let command: Command = new Command();
    command.CommandData = new ModifySettingsResponse();
    command = JSON.parse(message.data);
    if (command.CommandName == "AddServerSettingsResponse") {
      let response: ModifySettingsResponse = command.CommandData;
      if (response.Result == ModifySettingsResult.SETTING_ADDED) {
        let alert = this.alertCtrl.create({
          title: 'Setting Added',
          cssClass: 'userInactiveAlert',
          subTitle: '',
          buttons: ['OK']
        });
        alert.present();
        this.getServerSettingsForUser();
        this.generateNewServerSetting();
      }
      else
      {
        let alert = this.alertCtrl.create({
          title: 'ERROR: Setting Added',
          cssClass: 'userInactiveAlert',
          subTitle: response.Result,
          buttons: ['OK']
        });
        alert.present();       
      }
    }
  }
//#endregion

//#region "Start Stop Server"

startServer(i:number) {
  this.presentLoading(10000, "Start Server");
  this.assignFormGroupValuesToServerSettingByIndex(i);
  this.socket.next(new Command('StartServer', this.serverSettings[i]));
  this.socket.subscribe(message => {
    //LoggerService.log("Answer: " + message.data);
    this.callBackToStartServer(message, this.serverSettings[i]);
  });
}
private callBackToStartServer(message: any, serverSetting:ServerSetting) {
  let command: Command = new Command();
  command.CommandData = new StartServerResponse();
  command = JSON.parse(message.data);
  if (command.CommandName == "StartServerResponse") {
    this.dismissLoading();
    let response:StartServerResponse = command.CommandData;
    if (response.Result == StartServerResult.SERVER_STARTED) {
      let alert = this.alertCtrl.create({
        title: 'Server started',
        cssClass: 'userInactiveAlert',
        subTitle: '',
        buttons: ['OK']
      });
      alert.present();
      serverSetting.IsRunning = true;
      // this.getServerSettingsForUser();
      // this.generateNewServerSetting();
    }
    else
    {
      let alert = this.alertCtrl.create({
        title: 'ERROR: Server start',
        cssClass: 'userInactiveAlert',
        subTitle: response.Result,
        buttons: ['OK']
      });
      alert.present();       
    }
  }
}

stopServer(i:number) {
  this.presentLoading(40000, "Stopping Server");
  this.assignFormGroupValuesToServerSettingByIndex(i);
  this.socket.next(new Command('StopServer', this.serverSettings[i]));
  this.socket.subscribe(message => {
    //LoggerService.log("Answer: " + message.data);
    this.callBackToStopServer(message, this.serverSettings[i], i);
  });
}
private callBackToStopServer(message: any, serverSetting:ServerSetting, i:number) {
  let command: Command = new Command();
  command.CommandData = new StopServerResponse();
  command = JSON.parse(message.data);
  if (command.CommandName == "StopServerResponse") {
    this.dismissLoading();
    this.serverLogs[i] = "No Logs available";
    let response:StopServerResponse = command.CommandData;
    if (response.Result == StopServerResult.SERVER_STOPPED) {
      let alert = this.alertCtrl.create({
        title: 'Server stopped',
        cssClass: 'userInactiveAlert',
        subTitle: 'Normal shutdown',
        buttons: ['OK']
      });
      alert.present();
      serverSetting.IsRunning = false;
    }
    else if (response.Result == StopServerResult.SERVER_KILLED) {
      let alert = this.alertCtrl.create({
        title: 'Server stopped',
        cssClass: 'userInactiveAlert',
        subTitle: 'Killed due normal shutdown not possible',
        buttons: ['OK']
      });
      alert.present();
      serverSetting.IsRunning = false;
    }
    else
    {
      let alert = this.alertCtrl.create({
        title: 'ERROR: Server stop',
        cssClass: 'userInactiveAlert',
        subTitle: response.Result,
        buttons: ['OK']
      });
      alert.present();       
    }
  }
}
//#endregion

//#region "ServerLog"

toggleServerLog(i:number) {
  this.serverSettings[i].openServerLog = !this.serverSettings[i].openServerLog;
  if(this.serverSettings[i].openServerLog && this.serverLogs[i] == "No Logs available")
  {
    this.socket.next(new Command('ConnectTelnet', this.serverSettings[i]));
    this.socket.subscribe(message => {
      //LoggerService.log("Answer: " + message.data);
      this.callTelnetConnect(message, i);
    });    
  }
}
private callTelnetConnect(message: any, i:number) {
  let command: Command = new Command();
  command.CommandData = new ConnectTelnetResponse();
  command = JSON.parse(message.data);
  if (command.CommandName == "ConnectTelnetResponse") {
    let response:ConnectTelnetResponse = command.CommandData;
    if (response.Result == ConnectTelnetResult.OK) {
      this.serverLogs[i] = "Connected to server...\n";
      this.socket.subscribe(message => {
        //LoggerService.log("Answer: " + message.data);
        this.receiveTelnet(message, i);
      });         
    }
    else
    {
      let alert = this.alertCtrl.create({
        title: 'ERROR: Connect telnet',
        cssClass: 'userInactiveAlert',
        subTitle: response.Result,
        buttons: ['OK']
      });
      alert.present();       
    }
  }
}
private receiveTelnet(message: any, i:number) {
  let command: Command = new Command();
  command = JSON.parse(message.data);
  if (command.CommandName == "OnTelnetReceived") {
    this.serverLogs[i] += command.CommandData;
  }
}
//#endregion

//#region "Login"

  signin(){
    this.webSocketProvider.createNewWebSocket();
    this.socket = this.webSocketProvider.getInstance();
    this.socket.subscribe(message => {
      let command = new Command();
      command = JSON.parse(message.data);
      if(command.CommandName == "Connected"){
        this.processUserLogin();
      }
    });
  } 
  processUserLogin(){
    this.socket.subscribe(message => {
      let com:Command = new Command();
      try {
        com.CommandData = new BesteUserAuthentificationResponse();
        com = JSON.parse(message.data);
        if(com.CommandName != "AuthentificationResponse"){
          this.loggedIn = false;
          this.webSocketProvider.closeWebSocket();
        }
        else {
          let response:BesteUserAuthentificationResponse = com.CommandData;
          if(response.Result == "SUCCESS")
          {
            this.loggedIn = true;
            GlobalVarsSDaysTDie.loggedinUser = response.UserData;
            this.getServerSettingsForUser();
            this.generateNewServerSetting();
          }
          else if (response.Result == "MUST_CHANGE_PASSWORT")
          {
            GlobalVarsSDaysTDie.loggedinUser = response.UserData;
            let changePassword = this.modalCtrl.create(ChangePasswordPage, { user: response.UserData,
              text: "Ihr Passwort ist abgelaufen. Sie müssen Ihr Passwort daher jetzt ändern.",
              websocket: this.webSocketProvider },{ enableBackdropDismiss: false });
            changePassword.onDidDismiss(data => {
              this.loggedIn = true;
              this.getServerSettingsForUser();
              this.generateNewServerSetting();
            });
            changePassword.present();
          }
          else if(response.Result == "WRONG_PASSWORD")
          {
            let alert = this.alertCtrl.create({
              title: 'Wrong password',
              cssClass: 'userInactiveAlert',
              subTitle: 'You entered a wrong password!',
              buttons: ['OK']
            });
            alert.present();
          }
          else if(response.Result == "WRONG_PASSWORD_COUNTER_TOO_HIGH")
          {
            let alert = this.alertCtrl.create({
              title: 'Too many unsuccessful logins!',
              cssClass: 'userInactiveAlert',
              subTitle: 'The password was entered more than 10 times wrong!\nPlease contact the page administrator',
              buttons: ['OK']
            });
            alert.present();
          }
          else if(response.Result == "USER_UNKNOWN")
          {
            let alert = this.alertCtrl.create({
              title: 'The user is unknown',
              cssClass: 'userInactiveAlert',
              subTitle: 'The user is not known.\nPlease create a user or contact the page administrator',
              buttons: ['OK']
            });
            alert.present();
          }
          else
          {
            let alert = this.alertCtrl.create({
              title: 'Unknown Error occured',
              cssClass: 'userInactiveAlert',
              subTitle: 'An unknown error on login occured. Please contact the side administrator',
              buttons: ['OK']
            });
            alert.present();
            this.loggedIn = false;
            this.webSocketProvider.closeWebSocket();
          }
        }        
      }catch(e){
        //ERROR!!!!
      }


    });
    this.sendLoginData();
  }

  changePassword() {
    let changePassword = this.modalCtrl.create(ChangePasswordPage, { user: GlobalVarsSDaysTDie.loggedinUser,
     text: "Ändere dein Passwort",
     websocket: this.webSocketProvider },{ enableBackdropDismiss: false });
    changePassword.onDidDismiss(data => {
      this.loggedIn = true;
      this.getServerSettingsForUser();
      this.generateNewServerSetting();
    });
    changePassword.present();    
  }

  sendLoginData(){
    let com:Command = new Command();
    com.CommandName = "Login";
    let user:User = new User();
    user.Username = this.inputSignin.get('username').value;
    user.Password = this.inputSignin.get('password').value;
    com.CommandData = user;
    this.socket.next(com);
  }

  logout(){
    this.webSocketProvider.closeWebSocket();
    this.navCtrl.setRoot(SdaystdiePage, {}, {animate: true, direction: 'forward'});
  }

  //#endregion
}
