/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CompleteTaskResponseDto } from '../models/CompleteTaskResponseDto';
import type { CreateTaskDto } from '../models/CreateTaskDto';
import type { TodoTask } from '../models/TodoTask';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class TodoService {
    /**
     * @param requestBody
     * @returns TodoTask OK
     * @throws ApiError
     */
    public static postApiTodo(
        requestBody?: CreateTaskDto,
    ): CancelablePromise<TodoTask> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Todo',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns TodoTask OK
     * @throws ApiError
     */
    public static getApiTodo(): CancelablePromise<Array<TodoTask>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Todo',
        });
    }
    /**
     * @param id
     * @returns CompleteTaskResponseDto OK
     * @throws ApiError
     */
    public static postApiTodoComplete(
        id: number,
    ): CancelablePromise<CompleteTaskResponseDto> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Todo/{id}/complete',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @returns any OK
     * @throws ApiError
     */
    public static postApiTodoFinish(
        id: number,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Todo/{id}/finish',
            path: {
                'id': id,
            },
        });
    }
}
