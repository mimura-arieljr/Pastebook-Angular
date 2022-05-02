import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NgxSpinnerService } from 'ngx-spinner';
import { CurrentUserService } from '../current-user.service';

@Component({
  selector: 'app-timeline',
  templateUrl: './timeline.component.html',
  styleUrls: ['./timeline.component.scss']
})
export class TimelineComponent implements OnInit, AfterViewInit {
  @ViewChildren('lastPost', { read: ElementRef })
  lastPost!: QueryList<ElementRef>;


  timelinePosts: any = [];
  page: number = 1;
  isValidFileSize: boolean = true;
  newPostContent: string = '';
  base64Image: string = "";
  currentUser = {
    FirstName: '',
    LastName: '',
    Username: '',
    ImageSrc: '',
  };
  observer: any;
  visitedProfile: string | undefined = "";

  constructor(
    private currentUserService: CurrentUserService,
    private http: HttpClient,
    private spinner: NgxSpinnerService,
    private useParams: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.currentUserService.getCurrentUser();
    this.currentUserService.currentUser$.subscribe((user) => {
      this.currentUser = user;
    })
    this.useParams.paramMap.subscribe(paramMap => {
      this.visitedProfile = paramMap.get('username')?.toString()
    })
    this.getTimelinePosts()
    this.intersectionObserver()
  }

  ngAfterViewInit(): void {
    this.lastPost.changes.subscribe((d) => {
      if (d.last) this.observer.observe(d.last.nativeElement)
    })
  }

  handleFileSelect(evt: any) {
    var files = evt.target.files;
    var file = files[0];

    if (files && file) {
      var reader = new FileReader();

      reader.onload = (fileLoadedEvent: any) => {
        this.base64Image = fileLoadedEvent.target.result;
      }
      reader.readAsDataURL(file);
    }

  }

  resetCreatePost() {
    this.base64Image = '';
    this.newPostContent = '';
  }

  getTimelinePosts() {
    this.spinner.show()
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        })
      };
      this.http.get<string>(`http://localhost:5000/timelineposts/${this.visitedProfile}/${this.page}`, httpOptions)
        .subscribe((timelinePosts) => {
          this.spinner.hide()
          this.timelinePosts = [...this.timelinePosts, ...timelinePosts]
        })
    }
  }

  intersectionObserver() {
    let options = {
      root: null,
      threshold: 0.5,
    }

    this.observer = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting) {
        this.page++;
        this.getTimelinePosts()
      }
    }, options)
  }

  onSubmitPost() {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        })
      };

      let newPost = {
        Timeline: 0,
        Content: this.newPostContent,
        ImageSrc: this.base64Image
      }
      this.http.post<string>(`http://localhost:5000/createpost/${this.visitedProfile}`, newPost, httpOptions)
        .subscribe(recentPost => {
          this.timelinePosts = [recentPost, ...this.timelinePosts]
        })
    }
    this.resetCreatePost()
  }

  onDeletePost(id: number, index: number) {
    const JWT = localStorage.getItem('JSONWebToken')
    if (JWT) {
      const httpOptions = {
        headers: new HttpHeaders({
          'AuthToken': JWT
        }),
        responseType: 'text' as 'json'
      };

      this.http.delete(`http://localhost:5000/posts/${id}`, httpOptions)
        .subscribe(() => {
          this.timelinePosts.splice(index, 1)
        })
    }
  }

  convertTimestampToRelative(timestamp: number) {
    let start: number = Date.now()
    let type; // s, m, h, w
    let timeValue;
    const timeSecondsAgo = Math.floor((start - timestamp) / 1000)
    if (timeSecondsAgo < 60) {
      type = 's'
      timeValue = (timeSecondsAgo).toString()
      return "A few seconds ago"
    }
    else if (timeSecondsAgo >= 60 && timeSecondsAgo < 3600) {
      type = 'm'
      timeValue = (timeSecondsAgo / 60).toString().split(".")[0]
      return timeValue + type
    }
    else if (timeSecondsAgo >= 3600 && timeSecondsAgo < 86400) {
      type = 'h'
      timeValue = (timeSecondsAgo / 60 / 60).toString().split(".")[0]
      return timeValue + type
    }
    else if (timeSecondsAgo >= 86400 && timeSecondsAgo < 604800) {
      type = 'd'
      timeValue = (timeSecondsAgo / 60 / 60 / 24).toString().split(".")[0]
      return timeValue + type
    }
    else if (timeSecondsAgo >= 604800) {
      type = 'w'
      timeValue = (timeSecondsAgo / 60 / 60 / 24 / 7).toString().split(".")[0]
      return timeValue + type
    }
    else {
      return "Invalid time"
    }
  }
}
