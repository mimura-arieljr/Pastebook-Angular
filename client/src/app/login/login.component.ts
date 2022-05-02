import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { CurrentUserService } from '../current-user.service';
import { FetchService } from './fetch.service';
import { LoginModel } from './login.model';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  validCredentials: boolean = true;
  loginModelObj: LoginModel = new LoginModel();

  userForm = new FormGroup({
    Email: new FormControl('', [
      Validators.required,
      Validators.pattern(/^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})$/)
    ]),
    Password: new FormControl('', [
      Validators.required,
      Validators.minLength(8)
    ])
  });

  constructor(
    private service: FetchService,
    private currentUser: CurrentUserService) { }

  ngOnInit(): void {
    this.currentUser.checkLocalStorage();
  }

  get Email() {
    return this.userForm.get('Email');
  }

  get Password() {
    return this.userForm.get('Password');
  }

  submitForm() {
    this.loginModelObj.Email = this.userForm.value.Email;
    this.loginModelObj.Password = this.userForm.value.Password;

    this.service.postLogin(this.loginModelObj)
      .subscribe(res => {
        if(res === 'invalid')
        {
          this.validCredentials = false;
          return
        }
        localStorage.setItem('JSONWebToken', res)
        this.currentUser.isLoggedIn$.next(true);
        this.currentUser.getCurrentUser();
      })
  }

}
