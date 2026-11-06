
import http from 'k6/http';
import { check } from 'k6';

export const options = {
    vus: 10,
    duration: '30s',
    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<1000'],
    },
};

export default function () {
    const response = http.get(
        'http://host.docker.internal:5239/api/products',
        { timeout: '15s' }
    );

    check(response, {
        'status is 200': (r) => r.status === 200,
    });
}
