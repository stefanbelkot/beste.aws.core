import { Component, OnInit } from '@angular/core';
import { NavController, NavParams,ViewController  } from 'ionic-angular';
import { WebsocketProvider } from '../../providers/websocket-service';
import { Subject, Subscription } from 'rxjs';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Command, User, ModifyUserResponse } from '../../app/classes/classes';
import { GlobalVars } from '../../providers/globalVars';

@Component({
  selector: 'page-change-password',
  templateUrl: 'change-password.html',
})
export class ChangePasswordPage implements OnInit {

  inputChangePassword: FormGroup;
  socket:Subject<any>;
  user:User;
  text:string;
  passwordGuidelinesErrorMessage:string = "";
  logoutSubscription:Subscription = null;
  showAbort:boolean = false;

  constructor(public navCtrl: NavController, public navParams: NavParams, private viewCtrl:ViewController) {
    let websocket:WebsocketProvider = this.navParams.get('websocket');
    this.socket = websocket.getInstance();
    this.user = this.navParams.get('user');
    this.text = this.navParams.get('text');
    this.showAbort = this.navParams.get('showAbort') == "true" ? true : false;
    this.logoutSubscription = GlobalVars.inactiveTimoutObservable.subscribe(value => {
      this.dismiss();
    });
  }

  ngOnDestroy() {
    this.logoutSubscription.unsubscribe();
  }
  ngOnInit() {
		/* you can compose validators with Validators.compose([ ... ]) */
		this.inputChangePassword = new FormGroup({
			'password': new FormControl(null, Validators.required),
			'password-repeat': new FormControl(null, Validators.required),
			'save': new FormControl(true)
    });  
    this.socket.subscribe(message => {
      let com:Command = new Command();
      com.CommandData = new ModifyUserResponse();
      com = JSON.parse(message.data);
      let response:ModifyUserResponse = com.CommandData;
      
      if(com.CommandName == "ChangePasswordResponse")
      {
        if(response.Result == "SUCCESS")
        {
          this.viewCtrl.dismiss();
        }
        else if(response.Result == "PASSWORD_GUIDELINES_ERROR"){
          
          this.passwordGuidelinesErrorMessage = "Password guidelines error, please follow the rules:\n";
          this.passwordGuidelinesErrorMessage += response.PasswordRules.HasDigit == true ? "Must have a digit\n" : "";
          this.passwordGuidelinesErrorMessage += response.PasswordRules.HasLowerCase == true ? "Must have a lower case letter\n" : "";
          this.passwordGuidelinesErrorMessage += response.PasswordRules.HasSpecialChars == true ? "Must have a special char\n" : "";
          this.passwordGuidelinesErrorMessage += response.PasswordRules.HasUpperCase == true ? "Must have a upper case letter\n" : "";
          this.passwordGuidelinesErrorMessage += response.PasswordRules.MinLength > 0 ? "Must be longer than " + response.PasswordRules.MinLength + " chars\n" : "";
          this.inputChangePassword.setErrors({passwordGuidelinesError: true});
        }
      } 
    });

   }

  dismiss() {
    this.viewCtrl.dismiss();
  }


  changePassword(){
    let password:string = this.inputChangePassword.get('password').value;
    let passwordRepeat:string = this.inputChangePassword.get('password-repeat').value;
    
    if(password == passwordRepeat){
      let command:Command = new Command();
      command.CommandName = "ChangePassword";
      let passwordChangeUser:User = new User();
      passwordChangeUser.Username = this.user.Username;
      passwordChangeUser.Password = password;
      command.CommandData = passwordChangeUser;
      this.socket.next(command);
    }
    else{
      this.inputChangePassword.setErrors({passwordsNotMatching: true});
    }

  }
  abort() {
    this.viewCtrl.dismiss();
  }
}
