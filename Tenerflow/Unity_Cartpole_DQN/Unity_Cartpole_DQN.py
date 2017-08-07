import numpy as np
import tensorflow as tf
import random
from collections import deque
from dqn4 import dqn4 as dqn
#from dqn3 import dqn3 as dqn
#from dqn2 import dqn2 as dqn
#from dqn import dqn as dqn
from MySocket import MySocket
  
input_size = 4
output_size = 2

dis = 0.9
REPLAY_MEMORY = 50000
MAX_STEP = 500

cartpole = MySocket.MySocket()


def ddqn_replay_train(mainDQN, targetDQN, train_batch):
    x_stack = np.empty(0).reshape(0, mainDQN.input_size)
    y_stack = np.empty(0).reshape(0, mainDQN.output_size)

    for state, action, reward, next_state, done in train_batch:
        Q = mainDQN.predict(state)

        if done:
            Q[0, action] = reward
        else:
            Q[0, action] = reward + dis * targetDQN.predict(next_state)[0, np.argmax(mainDQN.predict(next_state))]

        y_stack = np.vstack([y_stack, Q])
        x_stack = np.vstack([x_stack, state])

    return mainDQN.update(x_stack, y_stack)

def get_copy_var_ops(*, dest_scope_name="target", src_scope_name="main"):

    op_holder = []

    src_vars = tf.get_collection(tf.GraphKeys.TRAINABLE_VARIABLES, scope=src_scope_name)
    dest_vars = tf.get_collection(tf.GraphKeys.TRAINABLE_VARIABLES, scope=dest_scope_name)

    for src_var, dest_var in zip(src_vars, dest_vars):
        op_holder.append(dest_var.assign(src_var.value()))

    return op_holder

def main():
    max_episodes = 5000
    replay_buffer = deque()
    last_100_game_reward = deque()

    with tf.Session() as sess:
        mainDQN = dqn.DQN(sess, input_size, output_size, name="main")
        targetDQN = dqn.DQN(sess, input_size, output_size, name="target")
        tf.global_variables_initializer().run()

        copy_ops = get_copy_var_ops(dest_scope_name="target", src_scope_name="main")
        sess.run(copy_ops)

        for episode in range(max_episodes):
            e = 1. / ((episode / 10) + 1)
            done = False
            step_count = 0
            state, _reward, _done = cartpole.getStep()
            while not done:
                if np.random.rand(1) < e:
                    action = np.random.randint(2)
                else:
                    action = np.argmax(mainDQN.predict(state))

                cartpole.sendingMsg(action)
                next_state, reward, done = cartpole.getStep()
                #print(next_state)
                #print("a : ",action, "s : ",next_state, "e : ", done)
                if done:
                    reward = -100
                    if step_count > MAX_STEP:
                        reward = -50
                    elif step_count > MAX_STEP * 0.8:
                        reward = -60
                    elif step_count > MAX_STEP * 0.6:
                        reward = -70
                    elif step_count > MAX_STEP * 0.4:
                        reward = -80
                    elif step_count > MAX_STEP * 0.2:
                        reward = -90


                replay_buffer.append((state, action, reward, next_state, done))
                if len(replay_buffer) > REPLAY_MEMORY:
                      replay_buffer.popleft()

                state = next_state
                step_count += 1

            print("Episode: {} steps: {}".format(episode, step_count))
            
            if episode % 4 == 1:
                for _ in range(100):
                    minibatch = random.sample(replay_buffer, 10)
                    loss, _ = ddqn_replay_train(mainDQN, targetDQN, minibatch)

                print("Loss: ", loss)

                sess.run(copy_ops)
            
            last_100_game_reward.append(step_count)

            if len(last_100_game_reward) > 100:
                last_100_game_reward.popleft()

                avg_reward = np.mean(last_100_game_reward)

                if avg_reward > MAX_STEP * 0.97:
                    print("Game Cleared")
                    break

if __name__ == "__main__":
    main()