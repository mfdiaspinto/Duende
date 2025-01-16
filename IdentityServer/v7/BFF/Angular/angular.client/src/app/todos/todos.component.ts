import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import {
  BehaviorSubject,
  catchError,
  filter,
  Observable,
  of,
  Subject,
  switchMap,
  tap,
} from 'rxjs';
import { AuthenticationService } from '../authentication.service';

@Component({
  selector: 'app-todos',
  templateUrl: './todos.component.html',
})
export class TodosComponent implements OnInit {
  private readonly todos = new BehaviorSubject<Todo[]>([]);
  public readonly todos$: Observable<Todo[]> = this.todos;

  private readonly errors = new BehaviorSubject<string>('');
  public readonly error$: Observable<string> = this.errors;

  public authenticated$ = this.auth.getIsAuthenticated();
  public anonymous$ = this.auth.getIsAnonymous();

  public date = new Date().toISOString().split('T')[0];
  public name = '';

  public constructor(
    private http: HttpClient,
    private auth: AuthenticationService
  ) {}

  public ngOnInit(): void {
    this.authenticated$
      .pipe(
        filter((isAuthenticated) => isAuthenticated),
        tap(() => {
          this.fetchTodos();
        })
      )
      .subscribe();
  }

  public createTodo(): void {
    let serverNumber = Math.random() < 0.5 ? 1 : 2;
    let server = serverNumber == 1 ? 'todos' : 'todos2';
    this.http
      .post<Todo>(server, {
        name: this.name,
        date: this.date,
      })
      .pipe(catchError(this.showError))
      .subscribe((todo) => {
        todo.server = serverNumber;
        const todos = [...this.todos.getValue(), todo];
        this.todos.next(todos);
      });
  }

  public deleteTodo(todo: Todo): void {
    let server = todo.server == 1 ? 'todos' : 'todos2';
    this.http
      .delete(`${server}/${todo.id}`)
      .pipe(catchError(this.showError))
      .subscribe(() => {
        const todos = this.todos
          .getValue()
          .filter(
            (x) =>
              x.id !== todo.id || (x.id === todo.id && x.server !== todo.server)
          );
        this.todos.next(todos);
      });
  }

  private fetchTodos(): void {
    this.http
      .get<Todo[]>('todos')
      .pipe(catchError(this.showError))
      .subscribe((todos) => {
        todos.forEach((o) => {
          o.server = 1;
        });

        this.http
          .get<Todo[]>('todos2')
          .pipe(catchError(this.showError))
          .subscribe((todos2) => {
            todos2.forEach((o) => {
              o.server = 2;
            });
            this.todos.next([...todos, ...todos2]);
          });
      });
  }

  private readonly showError = (err: HttpErrorResponse) => {
    if (err.status !== 401) {
      this.errors.next(err.message);
    }
    throw err;
  };
}

interface Todo {
  id: number;
  name: string;
  date: string;
  user: string;

  server: number;
}
