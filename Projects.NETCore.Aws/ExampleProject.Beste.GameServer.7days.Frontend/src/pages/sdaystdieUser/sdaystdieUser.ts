import { Component } from '@angular/core';
import { NavController, NavParams, ModalController, AlertController, LoadingController, Loading } from 'ionic-angular';
import { WebsocketProvider } from '../../providers/websocket-service';
import { Subject, Subscription } from 'rxjs';
import { Command, User, BesteUserAuthentificationResponse, ModifyUserResponse, ModifyUserResult, GetUsersResponse, GetUsersParams, SortUsersBy, GetUsersResult, Right, HasRightsResponse, HasRightsResult, BesteUserAuthentificationResult } from '../../app/classes/classes';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { ChangePasswordPage } from '../../modals/change-password/change-password';
import { GlobalVarsSDaysTDie } from '../../providers/globalVarsSDaysTDie';

@Component({
  selector: 'page-sdaystdieUser',
  templateUrl: 'sdaystdieUser.html'
})
export class SdaystdieUserPage {

  inputSignin: FormGroup;
  inputUsers: Array<FormGroup>;
  users: Array<User>;
  newInputUser: FormGroup;
  newUser: User;
  myInputUser: FormGroup;

  myUser: User = new User();

  socket:Subject<any>;
  impressumVisible:boolean = false;
  loggedIn:boolean = false;
  hasCreateUserRight:boolean = false;
  hasGetUsersRight:boolean = false;
  logoutSubscription:Subscription = null;

  rights:Array<Right>;

  loadingInstance:Loading;

  webSocketProvider:WebsocketProvider = GlobalVarsSDaysTDie.webSocketProvider;
  constructor(public navCtrl: NavController, public navParams: NavParams, public modalCtrl:ModalController,
    private alertCtrl:AlertController, private loadingController: LoadingController) {
    
    this.rights = [new Right("GetUsers", "User", null),
      new Right("CreateUser", "User", null),
      new Right("DeleteUser", "User", null),
      new Right("EditUser", "User", null)];
    this.loggedIn = GlobalVarsSDaysTDie.loggedinUser != null;  
    if(this.loggedIn)
    {
      this.socket = GlobalVarsSDaysTDie.webSocketProvider.getInstance();
      this.myInputUser = this.generateFormGroupByUser(GlobalVarsSDaysTDie.loggedinUser);
      this.getDataFromServer();
      setTimeout(() => {
        this.myUser.open = true;
      }, 100);
    }
    this.users = new Array<User>();
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
  getDataFromServer() {
    this.getRights().then(p =>{
      if(this.rights.find(p => (p.Action == "GetUsers" && p.HasRight)))
      {
        this.hasGetUsersRight = true;
        this.getUsers();
      }
      else
      {
        this.hasGetUsersRight = false;
      }
      if(this.rights.find(p => (p.Action == "CreateUser" && p.HasRight)))
      {
        this.hasCreateUserRight = true;
        this.generateNewUser();
      }
      else
      {
        this.hasCreateUserRight = false;
      }
    });
  }
  getRights():Promise<any> {
    return new Promise((resolve, reject) =>
    {
      this.socket.subscribe(message => {
        let command:Command = new Command();
        command.CommandData = new HasRightsResponse();
        command = JSON.parse(message.data);
        if(command.CommandName == "LoggedInUserHasRightsResponse"){
          let response:HasRightsResponse = command.CommandData;
          if(response.Result == HasRightsResult.SUCCESS)
          {
            this.rights = response.Rights;
            resolve();
          }
          else
          {
            reject();
          }
        }
      });
      let command:Command = new Command();
      command.CommandName = "LoggedInUserHasRights";
      command.CommandData = this.rights;
      this.socket.next(command);
    });    
  }
//#region "Users"

  getUsers():Promise<any>{
    return new Promise((resolve, reject) =>
    {
      this.socket.subscribe(message => {
        //LoggerService.log("Answer: " + message.data);
        let command:Command = new Command();
        command.CommandData = new GetUsersResponse();
        command = JSON.parse(message.data);
        if(command.CommandName == "GetUsersResponse"){
          let resonse:GetUsersResponse = command.CommandData;
          if(resonse.Result == GetUsersResult.SUCCESS)
          {
            this.generateFormGroupForInputUsers(resonse.Users);
            this.users = resonse.Users;
            resolve();
          }
          else
          {
            reject();
          }
        }
      });
      let command:Command = new Command();
      command.CommandName = "GetUsers";
      let getUserParams:GetUsersParams = new GetUsersParams();
      getUserParams.Limit = 100;
      getUserParams.Offset = 0;
      getUserParams.SortUsersBy = SortUsersBy.USERNAME;
      command.CommandData = getUserParams;
      this.socket.next(command);
    });
  }

  toggleSection(i:number) {
    this.users[i].open = !this.users[i].open;
  }

  generateFormGroupForInputUsers(inputUsers:Array<User>)
  {
    this.inputUsers = new Array<FormGroup>();   
    for (var i = 0, len = inputUsers.length; i < len; i++) {
      this.inputUsers.push(this.generateFormGroupByUser(inputUsers[i]));
    }
  }

  generateFormGroupByUser(user:User) : FormGroup
  {
    let userGroup:FormGroup = new FormGroup({
			'firstname': new FormControl(null),
			'lastname': new FormControl(null),
			'username': new FormControl(null),
			'password': new FormControl(null),
			'email': new FormControl(null)  
    });
    userGroup.setValue({
      'firstname':user.Firstname,
      'lastname':user.Lastname,
      'email':user.Email,
      'username':user.Username,
      'password':user.Password == null ? "" : user.Password
    });
    return userGroup;
  }
  
  deleteUser(i:number) {
    let alert = this.alertCtrl.create({
      title: 'Delete User',
      cssClass: 'userInactiveAlert',
      subTitle: "Do you really want to delete the User: '" + this.users[i].Username + "'?",
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
              this.callBackToDeleteUser(message);
            });
            this.socket.next(new Command('DeleteUser', this.users[i]));
          }
        }
      ]
    });
    alert.present();
  }

  private callBackToDeleteUser(message: any) {
    let command: Command = new Command();
    command.CommandData = new ModifyUserResponse();
    command = JSON.parse(message.data);
    if (command.CommandName == "DeleteUserResponse") {
      let response: ModifyUserResponse = command.CommandData;
      if (response.Result == ModifyUserResult.SUCCESS) {
        let alert = this.alertCtrl.create({
          title: 'User deleted',
          cssClass: 'userInactiveAlert',
          subTitle: '',
          buttons: ['OK']
        });
        alert.present();
        this.getDataFromServer();
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

  updateMyUser() {
    GlobalVarsSDaysTDie.loggedinUser.Username = this.myInputUser.get('username').value;
    GlobalVarsSDaysTDie.loggedinUser.Lastname = this.myInputUser.get('lastname').value;
    GlobalVarsSDaysTDie.loggedinUser.Firstname = this.myInputUser.get('firstname').value;
    GlobalVarsSDaysTDie.loggedinUser.Email = this.myInputUser.get('email').value;
    GlobalVarsSDaysTDie.loggedinUser.Password = "";
    this.socket.next(new Command('EditUser', GlobalVarsSDaysTDie.loggedinUser));
    this.socket.subscribe(message => {
      //LoggerService.log("Answer: " + message.data);
      this.callBackToEditUser(message);
    });   
  }
  
  updateUser(i:number) {
    this.assignFormGroupValuesToUserByIndex(i);
    this.socket.next(new Command('EditUser', this.users[i]));
    this.socket.subscribe(message => {
      //LoggerService.log("Answer: " + message.data);
      this.callBackToEditUser(message);
    });
  }

  private callBackToEditUser(message: any) {
    let command: Command = new Command();
    command.CommandData = new ModifyUserResponse();
    command = JSON.parse(message.data);
    if (command.CommandName == "EditUserResponse") {
      let response:ModifyUserResponse = command.CommandData;
      if (response.Result == ModifyUserResult.SUCCESS) {
        let alert = this.alertCtrl.create({
          title: 'User updated',
          cssClass: 'userInactiveAlert',
          subTitle: '',
          buttons: ['OK']
        });
        alert.present();
      }
      else
      {
        let alert = this.alertCtrl.create({
          title: 'ERROR: User updated',
          cssClass: 'userInactiveAlert',
          subTitle: response.Result,
          buttons: ['OK']
        });
        alert.present();       
      }
    }
  }

  assignFormGroupValuesToUserByIndex(i:number)
  {
    this.users[i].Username = this.inputUsers[i].get('username').value;
    this.users[i].Lastname = this.inputUsers[i].get('lastname').value;
    this.users[i].Firstname = this.inputUsers[i].get('firstname').value;
    this.users[i].Email = this.inputUsers[i].get('email').value;
    this.users[i].Password = this.inputUsers[i].get('password').value;
    this.users[i].MustChangePassword = true;
  }
  
  toggleNewUserSection() {
    this.newUser.open = !this.newUser.open;
  }
  toggleMyUserSection() {
    this.myUser.open = !this.myUser.open;
  }
  generateNewUser() {
    this.newUser = new User();
    this.newInputUser = this.generateFormGroupByUser(this.newUser);
  }

  addUser() {
    this.assignNewFormGroupValuesToNewUser();
    this.socket.subscribe(message => {
      //LoggerService.log("Answer: " + message.data);
      this.callBackToAddUser(message);
    });
    this.socket.next(new Command('CreateUser', this.newUser));
  }

  assignNewFormGroupValuesToNewUser()
  {
    this.newUser.Username = this.newInputUser.get('username').value;
    this.newUser.Lastname = this.newInputUser.get('lastname').value;
    this.newUser.Firstname = this.newInputUser.get('firstname').value;
    this.newUser.Email = this.newInputUser.get('email').value;
    this.newUser.Password = this.newInputUser.get('password').value;
  }

  private callBackToAddUser(message: any) {
    let command: Command = new Command();
    command.CommandData = new ModifyUserResponse();
    command = JSON.parse(message.data);
    if (command.CommandName == "CreateUserResponse") {
      let response: ModifyUserResponse = command.CommandData;
      if (response.Result == ModifyUserResult.SUCCESS) {
        let alert = this.alertCtrl.create({
          title: 'User Added',
          cssClass: 'userInactiveAlert',
          subTitle: '',
          buttons: ['OK']
        });
        alert.present();
        this.getDataFromServer();
      }
      else
      {
        let alert = this.alertCtrl.create({
          title: 'ERROR: User Added',
          cssClass: 'userInactiveAlert',
          subTitle: response.Result,
          buttons: ['OK']
        });
        alert.present();       
      }
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
          if(response.Result == BesteUserAuthentificationResult.SUCCESS)
          {
            this.getDataFromServer();
            this.loggedIn = true;
            GlobalVarsSDaysTDie.loggedinUser = response.UserData;
            this.myInputUser = this.generateFormGroupByUser(GlobalVarsSDaysTDie.loggedinUser);
            setTimeout(() => {
              this.myUser.open = true;
            }, 100);
          }
          else if (response.Result == BesteUserAuthentificationResult.MUST_CHANGE_PASSWORT)
          {
            GlobalVarsSDaysTDie.loggedinUser = response.UserData;
            let changePassword = this.modalCtrl.create(ChangePasswordPage, { user: response.UserData,
              text: "Your password expired. Please change it.",
              websocket: this.webSocketProvider },{ enableBackdropDismiss: false });
            changePassword.onDidDismiss(data => {
              this.loggedIn = true;
              this.getDataFromServer();
              this.myInputUser = this.generateFormGroupByUser(GlobalVarsSDaysTDie.loggedinUser);
              setTimeout(() => {
                this.myUser.open = true;
              }, 100);
            });
            changePassword.present();
          }
          else if(response.Result == BesteUserAuthentificationResult.WRONG_PASSWORD)
          {
            let alert = this.alertCtrl.create({
              title: 'Wrong password',
              cssClass: 'userInactiveAlert',
              subTitle: 'You entered a wrong password!',
              buttons: ['OK']
            });
            alert.present();
          }
          else if(response.Result == BesteUserAuthentificationResult.WRONG_PASSWORD_COUNTER_TOO_HIGH)
          {
            let alert = this.alertCtrl.create({
              title: 'Too many unsuccessful logins!',
              cssClass: 'userInactiveAlert',
              subTitle: 'The password was entered more than 10 times wrong!\nPlease contact the page administrator',
              buttons: ['OK']
            });
            alert.present();
          }
          else if(response.Result == BesteUserAuthentificationResult.USER_UNKNOWN)
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
              subTitle: 'An error on login occured: ' + response.Result + '. Please contact the side administrator',
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
      text: "Change your password",
      websocket: this.webSocketProvider,
      showAbort: "true" },{ enableBackdropDismiss: false });
    changePassword.onDidDismiss(data => {
      this.loggedIn = true;
      this.getDataFromServer();
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

    //LoggerService.log("Login Command: " + JSON.stringify(com));
    let x:string = JSON.stringify(com);
    //LoggerService.log("WS-message: " + x);
    this.socket.next(com);
    //this.websocketService.startPing();
  }

  logout(){
    this.webSocketProvider.closeWebSocket();
    this.navCtrl.setRoot(SdaystdieUserPage, {}, {animate: true, direction: 'forward'});
  }

  //#endregion
}
